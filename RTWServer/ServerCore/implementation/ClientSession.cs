using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using Microsoft.Extensions.Logging;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class ClientSession : IClientSession
{
    // Network buffer size for packet reading
    private const int NETWORK_BUFFER_SIZE = 4096;
    
    // Connection state constants
    private const int CONNECTION_STATE_DISCONNECTED = 0;
    private const int CONNECTION_STATE_CONNECTED = 1;

    private readonly IClient _client;
    private readonly PipeWriter _writer;
    private readonly IPacketHandler _packetHandler;
    private readonly IPacketSerializer _packetSerializer;
    private readonly IClientSessionManager _clientSessionManager;
    private readonly ILogger _logger;

    private readonly ConcurrentQueue<IPacket> _sendQueue = new();
    private readonly Lock _sendLock = new();

    private bool _isSending;
    private int _connectionState; // CONNECTION_STATE_CONNECTED or CONNECTION_STATE_DISCONNECTED

    public string Id { get; private init; }

    public ClientSession(
        IClient client, 
        IPacketHandler packetHandler, 
        IPacketSerializer packetSerializer,
        IClientSessionManager clientSessionManager, 
        ILoggerFactory loggerFactory,
        string id)
    {
        _client = client;

        StreamPipeWriterOptions options = new(leaveOpen: true);
        _writer = PipeWriter.Create(client.Stream, options);

        _packetHandler = packetHandler;
        _packetSerializer = packetSerializer;
        _clientSessionManager = clientSessionManager;
        _logger = loggerFactory.CreateLogger<ClientSession>();

        Id = id;
        _isSending = false;
        Interlocked.Exchange(ref _connectionState, CONNECTION_STATE_CONNECTED);
    }

    public async Task StartSessionAsync(CancellationToken token)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(NETWORK_BUFFER_SIZE);
        _logger.LogDebug("Session started for client {ClientId}", Id);

        try
        {
            while (!token.IsCancellationRequested)
            {
                IPacket? packet = await ReadPacketAsync(buffer);
                if (packet == null)
                {
                    _logger.LogDebug("Null packet received, ending session for client {ClientId}", Id);
                    break;
                }

                _logger.LogTrace("Handling packet {PacketId} for client {ClientId}", packet.PacketId, Id);
                await _packetHandler.HandlePacketAsync(packet, this);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Session cancelled for client {ClientId}", Id);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Network error for client {ClientId}", Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in session for client {ClientId}", Id);
            throw;
        }
        finally
        {
            _logger.LogDebug("Removing session for client {ClientId}", Id);
            _clientSessionManager.RemoveClientSession(Id);

            ArrayPool<byte>.Shared.Return(buffer);

            await Disconnect();
        }
    }

    public async Task SendAsync(IPacket packet)
    {
        if (Interlocked.CompareExchange(ref _connectionState, CONNECTION_STATE_CONNECTED, CONNECTION_STATE_CONNECTED) == CONNECTION_STATE_DISCONNECTED)
        {
            // 이미 연결이 끊긴 경우 접근했으므로 세션 제거
            _logger.LogDebug("Attempted to send packet to disconnected client {ClientId}", Id);
            _clientSessionManager.RemoveClientSession(Id);
            return;
        }

        _logger.LogTrace("Enqueueing packet {PacketId} for client {ClientId}", packet.PacketId, Id);
        _sendQueue.Enqueue(packet);

        await FlushSendQueueAsync();
    }

    private async Task Disconnect()
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
            await _writer.CompleteAsync();
            _client.Close();
            _logger.LogInformation("Client {ClientId} disconnected successfully", Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during disconnect for client {ClientId}", Id);
        }
    }

    private async Task<IPacket?> ReadPacketAsync(byte[] buffer)
    {
        // 헤더 읽기
        int headerSize = _packetSerializer.GetHeaderSize();
        bool isReadHeader = await ReadBytesAsync(buffer, headerSize, 0);
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
        bool isReadPayload = await ReadBytesAsync(buffer, payloadSize, headerSize);
        if (!isReadPayload)
        {
            _logger.LogTrace("Failed to read payload for client {ClientId}", Id);
            return null;
        }

        // 패킷 생성
        _logger.LogTrace("Successfully read packet with payload size {PayloadSize} for client {ClientId}", payloadSize, Id);
        return _packetSerializer.Deserialize(buffer);
    }

    private async Task<bool> ReadBytesAsync(byte[] buffer, int sizeToRead, int offset)
    {
        if (sizeToRead > buffer.Length)
        {
            return false;
        }

        // 남은 데이터를 모두 읽을 때까지 반복
        while (sizeToRead > 0)
        {
            int sizeReceived = await _client.ReceiveAsync(buffer, offset, sizeToRead);
            if (sizeReceived == 0)
            {
                return false;
            }

            offset += sizeReceived;
            sizeToRead -= sizeReceived;
        }

        return true;
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

        while (true)
        {
            if (!_sendQueue.TryDequeue(out IPacket? packet))
            {
                lock (_sendLock)
                {
                    // 한번 더 확인하는 이유 - 아래 조건문이 없는 경우
                    // 1. 현재 스레드: Dequeue 실패,lock 대기
                    // 2. 다른 스레드: Enqueue 후 첫번째 lock 획득, isSending 이 참이므로 반환
                    // 3. 현재 스레드: lock 획득, isSending = false 후 반환
                    // 2번에서 Enqueue한 패킷 기아
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
                await FlushAsync(packet);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Network error while sending packet to client {ClientId}", Id);

                // 연결이 끊긴 경우 isSending을 false로 변경할 필요 없음
                await Disconnect();
                return;
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, "Connection already closed for client {ClientId}", Id);
                await Disconnect();
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending packet to client {ClientId}", Id);

                // 연결이 끊긴 경우 isSending을 false로 변경할 필요 없음
                await Disconnect();
                return;
            }
        }
    }

    private async Task FlushAsync(IPacket packet)
    {
        int headerSize = _packetSerializer.GetHeaderSize();
        int payloadSize = packet.GetPayloadSize();

        Span<byte> buffer = _writer.GetSpan(headerSize + payloadSize);

        _packetSerializer.SerializeToBuffer(packet, buffer);

        _writer.Advance(headerSize + payloadSize);

        await _writer.FlushAsync();
    }
}
