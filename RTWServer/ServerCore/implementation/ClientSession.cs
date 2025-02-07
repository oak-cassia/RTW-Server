using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class ClientSession : IClientSession
{
    private const int BUFFER_SIZE = 4096;

    private readonly IClient _client;
    private readonly PipeWriter _writer;
    private readonly IPacketHandler _packetHandler;
    private readonly IPacketSerializer _packetSerializer;
    private readonly IClientSessionManager _clientSessionManager;

    private readonly ConcurrentQueue<IPacket> _sendQueue = new();
    private readonly Lock _sendLock = new();

    private bool _isSending;
    private bool _isConnected;

    public string Id { get; private init; }

    public ClientSession(IClient client, IPacketHandler packetHandler, IPacketSerializer packetSerializer,
        IClientSessionManager clientSessionManager, string id)
    {
        _client = client;

        StreamPipeWriterOptions options = new(leaveOpen: true);
        _writer = PipeWriter.Create(client.Stream, options);

        _packetHandler = packetHandler;
        _packetSerializer = packetSerializer;
        _clientSessionManager = clientSessionManager;

        Id = id;
        _isSending = false;
        _isConnected = true;
    }

    public async Task StartSessionAsync(CancellationToken token)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);

        try
        {
            while (!token.IsCancellationRequested)
            {
                IPacket? packet = await ReadPacketAsync(buffer);
                if (packet == null)
                {
                    break;
                }

                await _packetHandler.HandlePacketAsync(packet, this);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _clientSessionManager.RemoveClientSession(Id);

            ArrayPool<byte>.Shared.Return(buffer);

            await Disconnect();
        }
    }

    public async Task SendAsync(IPacket packet)
    {
        if (!_isConnected)
        {
            // 이미 연결이 끊긴 경우 접근했으므로 세션 제거
            _clientSessionManager.RemoveClientSession(Id);

            return;
        }

        _sendQueue.Enqueue(packet);

        await FlushSendQueueAsync();
    }

    private async Task Disconnect()
    {
        if (!_isConnected)
        {
            return;
        }

        _isConnected = false;

        await _writer.CompleteAsync();

        _client.Close();
    }

    private async Task<IPacket?> ReadPacketAsync(byte[] buffer)
    {
        // 헤더 읽기
        int headerSize = _packetSerializer.GetHeaderSize();
        bool isReadHeader = await ReadBytesAsync(buffer, headerSize, 0);
        if (!isReadHeader)
        {
            return null;
        }

        // 페이로드 길이 확인
        int payloadSize = _packetSerializer.GetPayloadSizeFromHeader(buffer.AsSpan(0, headerSize));
        if (payloadSize < 0 || headerSize + payloadSize > BUFFER_SIZE)
        {
            return null;
        }

        // 페이로드 읽기
        bool isReadPayload = await ReadBytesAsync(buffer, payloadSize, headerSize);
        if (!isReadPayload)
        {
            return null;
        }

        // 패킷 생성
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
                        return;
                    }
                }
            }

            try
            {
                await FlushAsync(packet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

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