using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using Microsoft.Extensions.Logging;
using NetworkDefinition.ErrorCode;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class ClientSession : IClientSession
{
    // 패킷 읽기용 네트워크 버퍼 크기
    private const int NETWORK_BUFFER_SIZE = 4096;

    // 수신 측이 느려 송신 큐가 이 크기를 넘으면 무한정 메모리가 쌓이지 않도록 세션을 종료한다
    private const int SEND_QUEUE_LIMIT = 256;

    private const int CONNECTION_STATE_DISCONNECTED = 0;
    private const int CONNECTION_STATE_CONNECTED = 1;

    // 프로세스 내에서 충돌 없는 PlayerId 발급용 카운터 (string.GetHashCode는 충돌 가능 + 프로세스마다 값이 달라짐)
    private static int _lastPlayerId;

    private readonly IClient _client;
    private readonly PipeWriter _writer;
    private readonly IPacketHandler _packetHandler;
    private readonly IPacketSerializer _packetSerializer;
    private readonly ILogger _logger;

    private readonly ConcurrentQueue<IPacket> _sendQueue = new();
    private readonly Lock _sendLock = new();
    private readonly CancellationTokenSource _sessionCts = new(); // 세션 전용 취소 토큰 소스

    private bool _isSending;
    private int _connectionState;
    private int _isDisposed;

    public string Id { get; private init; } // 세션 ID이며 플레이어 ID로도 사용됨
    public int PlayerId { get; }
    public string? AuthToken { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public ClientSession(
        IClient client,
        IPacketHandler packetHandler,
        IPacketSerializer packetSerializer,
        ILoggerFactory loggerFactory,
        string id)
    {
        _client = client;

        StreamPipeWriterOptions options = new(leaveOpen: true);
        _writer = PipeWriter.Create(client.Stream, options);

        _packetHandler = packetHandler;
        _packetSerializer = packetSerializer;
        _logger = loggerFactory.CreateLogger<ClientSession>();

        Id = id;
        PlayerId = Interlocked.Increment(ref _lastPlayerId);
        _isSending = false;
        Interlocked.Exchange(ref _connectionState, CONNECTION_STATE_CONNECTED);
        IsAuthenticated = false; // 인증되지 않은 상태로 초기화
    }

    public async Task StartSessionAsync(CancellationToken token)
    {
        _logger.LogDebug("Session started for client {ClientId}", Id);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(NETWORK_BUFFER_SIZE);
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _sessionCts.Token);
            while (!linkedCts.Token.IsCancellationRequested)
            {
                IPacket? packet = await ReadPacketAsync(buffer, linkedCts.Token);
                if (packet == null)
                {
                    _logger.LogDebug("Null packet received or read operation cancelled, ending session for client {ClientId}", Id);
                    break;
                }

                _logger.LogTrace("Handling packet {PacketId} for client {ClientId}", packet.PacketId, Id);
                await _packetHandler.HandlePacketAsync(packet, this);
            }
        }
        catch (OperationCanceledException)
        {
            // 외부 토큰 또는 세션 자체의 RequestShutdown으로 취소되었는지 로깅
            if (token.IsCancellationRequested)
            {
                _logger.LogInformation("Session cancelled for client {ClientId} due to server shutdown.", Id);
            }
            else if (_sessionCts.IsCancellationRequested)
            {
                _logger.LogInformation("Session {ClientId} shut down as per request.", Id);
            }
            else
            {
                _logger.LogInformation("Session cancelled for client {ClientId}.", Id); // 일반 취소
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Network error for client {ClientId}", Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in session for client {ClientId}. Session will assume disconnected.", Id);
        }
        finally
        {
            _logger.LogDebug("Cleaning up session for client {ClientId}", Id);
            // _connectionState 경합을 방지하기 위해 매니저에서 제거하기 전에 Disconnect를 호출합니다.
            await DisconnectAsync();

            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async Task SendAsync(IPacket packet)
    {
        if (Volatile.Read(ref _connectionState) == CONNECTION_STATE_DISCONNECTED)
        {
            _logger.LogDebug("Attempted to send packet to disconnected client {ClientId}", Id);
            return;
        }

        _logger.LogTrace("Enqueueing packet {PacketId} for client {ClientId}", packet.PacketId, Id);
        _sendQueue.Enqueue(packet);

        // 수신이 막힌 클라이언트의 큐가 무한정 커지지 않도록 상한을 둔다
        if (_sendQueue.Count > SEND_QUEUE_LIMIT)
        {
            _logger.LogWarning("Send queue overflow for client {ClientId}, requesting shutdown", Id);
            await RequestShutdownAsync("Send queue overflow (slow consumer)");
            return;
        }

        // 전송 완료를 기다리지 않는다. 느린 수신자의 TCP 백프레셔가 호출자(브로드캐스트,
        // 발신자 세션의 수신 루프)를 막지 않도록 큐 적재와 실제 IO를 분리한다.
        // FlushSendQueueAsync는 내부에서 모든 예외를 처리하고 실패 시 세션 종료를 요청한다.
        _ = FlushSendQueueAsync();
    }

    private async Task DisconnectAsync()
    {
        // 이미 연결 해제된 경우 early return
        if (Interlocked.CompareExchange(ref _connectionState, CONNECTION_STATE_DISCONNECTED, CONNECTION_STATE_CONNECTED) == CONNECTION_STATE_DISCONNECTED)
        {
            _logger.LogTrace("Disconnect called on already disconnected client {ClientId}", Id);
            return;
        }

        _logger.LogDebug("Disconnecting client {ClientId}", Id);
        try
        {
            await _sessionCts.CancelAsync(); // 세션 루프 종료 신호
        }
        catch (ObjectDisposedException)
        {
            _logger.LogTrace("Session CTS already disposed for client {ClientId} during disconnect", Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cancelling session CTS for client {ClientId}", Id);
        }

        try
        {
            await _writer.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error completing writer for client {ClientId}", Id);
        }

        try
        {
            _client.Dispose();
            _logger.LogInformation("Client {ClientId} disconnected successfully", Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during disconnect for client {ClientId}", Id);
        }
    }

    private async Task<IPacket?> ReadPacketAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        // 헤더 읽기
        int headerSize = _packetSerializer.GetHeaderSize();
        bool isReadHeader = await ReadBytesAsync(buffer, headerSize, 0, cancellationToken);
        if (!isReadHeader)
        {
            _logger.LogTrace("Failed to read header for client {ClientId}", Id);
            return null;
        }

        // 페이로드 길이 확인
        int payloadSize = _packetSerializer.GetPayloadSizeFromHeader(buffer.AsSpan(0, headerSize));
        if (payloadSize < 0 || headerSize + payloadSize > NETWORK_BUFFER_SIZE)
        {
            _logger.LogWarning("Invalid payload size {PayloadSize} for client {ClientId}", payloadSize, Id);
            return null;
        }

        // 페이로드 읽기
        bool isReadPayload = await ReadBytesAsync(buffer, payloadSize, headerSize, cancellationToken);
        if (!isReadPayload)
        {
            _logger.LogTrace("Failed to read payload for client {ClientId}", Id);
            return null;
        }

        // 패킷 생성
        _logger.LogTrace("Successfully read packet with payload size {PayloadSize} for client {ClientId}", payloadSize, Id);
        return _packetSerializer.Deserialize(buffer);
    }

    private async Task<bool> ReadBytesAsync(byte[] buffer, int sizeToRead, int offset, CancellationToken cancellationToken)
    {
        if (sizeToRead > buffer.Length)
        {
            return false;
        }

        // 남은 데이터를 모두 읽을 때까지 반복
        while (sizeToRead > 0)
        {
            int sizeReceived = await _client.ReceiveAsync(buffer, offset, sizeToRead, cancellationToken);
            if (sizeReceived == 0)
            {
                return false;
            }

            offset += sizeReceived;
            sizeToRead -= sizeReceived;
        }

        return true;
    }

    public async Task RequestShutdownAsync(string reason)
    {
        _logger.LogInformation("Shutdown requested for session {SessionId}. Reason: {Reason}", Id, reason);
        try
        {
            if (!_sessionCts.IsCancellationRequested)
            {
                await _sessionCts.CancelAsync();
            }
        }
        catch (ObjectDisposedException)
        {
            // 이미 Disposed 상태면 추가 조치 불필요
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
        {
            return;
        }

        // 세션이 아직 종료되지 않은 경우 연결을 확실히 끊고 리소스를 정리합니다.
        await DisconnectAsync();
        _sessionCts.Dispose();
    }

    private async Task FlushSendQueueAsync()
    {
        lock (_sendLock)
        {
            if (_isSending)
            {
                return;
            }

            _isSending = true;
            _logger.LogTrace("Started sending packets for client {ClientId}", Id);
        }

        void MarkSendingComplete()
        {
            lock (_sendLock)
            {
                _isSending = false;
            }
        }

        while (true)
        {
            if (Volatile.Read(ref _connectionState) == CONNECTION_STATE_DISCONNECTED)
            {
                MarkSendingComplete();
                _logger.LogTrace("Send loop exits because client {ClientId} is disconnected", Id);
                return;
            }

            if (!_sendQueue.TryDequeue(out IPacket? packet))
            {
                lock (_sendLock)
                {
                    if (!_sendQueue.TryDequeue(out packet))
                    {
                        _isSending = false;
                        _logger.LogTrace("No more packets to send for client {ClientId}", Id);
                        return;
                    }
                }
            }

            try
            {
                _logger.LogTrace("Sending packet {PacketId} to client {ClientId}", packet.PacketId, Id);
                await FlushAsync(packet, _sessionCts.Token);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Network error while sending packet to client {ClientId}", Id);
                MarkSendingComplete();
                await RequestShutdownAsync($"Network error during send: {ex.GetType().Name}");
                return;
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, "Connection already closed for client {ClientId}", Id);
                MarkSendingComplete();
                await RequestShutdownAsync($"Connection already closed during send: {ex.GetType().Name}");
                return;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Send loop cancelled for client {ClientId}", Id);
                MarkSendingComplete();
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending packet to client {ClientId}", Id);
                MarkSendingComplete();
                await RequestShutdownAsync($"Unexpected error during send: {ex.GetType().Name}");
                return;
            }

            // 연결이 끊긴 경우 isSending을 false로 변경할 필요 없음
        }
    }

    private async Task FlushAsync(IPacket packet, CancellationToken cancellationToken)
    {
        int headerSize = _packetSerializer.GetHeaderSize();
        int payloadSize = packet.GetPayloadSize();

        Span<byte> buffer = _writer.GetSpan(headerSize + payloadSize);

        _packetSerializer.SerializeToBuffer(packet, buffer);

        _writer.Advance(headerSize + payloadSize);

        await _writer.FlushAsync(cancellationToken);
    }

    public async Task<(RTWErrorCode ErrorCode, int PlayerId)> ValidateAuthTokenAsync(string authToken)
    {
        // authToken은 자격 증명이므로 로그에 원문을 남기지 않는다
        _logger.LogDebug("Validating auth token for session {SessionId}", Id);

        // TODO: authToken을 영속 저장소(예: Redis, DB)에서 검증해야 한다.
        // 현재는 비어 있지 않은 토큰을 모두 통과시키는 스텁이다. 웹 서버 세션(session_{userId})과
        // 대조하려면 CAuthToken에 userId 필드가 필요하다 (proto 변경 필요).
        if (!string.IsNullOrEmpty(authToken))
        {
            AuthToken = authToken;
            IsAuthenticated = true;

            _logger.LogInformation("Auth token validated successfully for session {SessionId}. Effective PlayerId for packet: {PlayerId}", Id, PlayerId);
            return (RTWErrorCode.Success, PlayerId);
        }

        _logger.LogWarning("Auth token validation failed for session {SessionId}. Token was null or empty.", Id);
        IsAuthenticated = false;

        await Task.CompletedTask;
        return (RTWErrorCode.AuthenticationFailed, 0);
    }
}
