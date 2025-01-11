using System.Collections.Concurrent;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class ClientSession
{
    private const int BUFFER_SIZE = 4096;
    private const int HEADER_SIZE = 8;
    private const int HEADER_PACKET_ID_OFFSET = 0;
    private const int HEADER_LENGTH_OFFSET = 4;

    private readonly IClient _client;
    private readonly IPacketHandler _packetHandler;
    private readonly IPacketFactory _packetFactory;

    private readonly ConcurrentQueue<IPacket> _sendQueue = new();
    private readonly Lock _sendLock = new();

    private bool _isSending;

    public string Id { get; private init; }

    public ClientSession(IClient client, IPacketHandler packetHandler, IPacketFactory packetFactory, string id)
    {
        _client = client;
        _packetHandler = packetHandler;
        _packetFactory = packetFactory;
        Id = id;
        _isSending = false;
    }

    public async Task StartSessionAsync(CancellationToken token)
    {
        try
        {
            var buffer = new byte[BUFFER_SIZE];

            while (!token.IsCancellationRequested)
            {
                var packet = await ReadPacketAsync(buffer);
                if (packet == null)
                {
                    break;
                }

                await _packetHandler.HandlePacketAsync(packet, _client);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task SendAsync(IPacket packet)
    {
        _sendQueue.Enqueue(packet);
        
        await FlushSendQueueAsync();
    }

    private async Task<IPacket?> ReadPacketAsync(byte[] buffer)
    {
        // 헤더 읽기
        var isReadHeader = await ReadBytesAsync(buffer, HEADER_SIZE, 0);
        if (!isReadHeader)
        {
            return null;
        }

        // 페이로드 길이 확인
        var payloadLength = BitConverter.ToInt32(buffer, HEADER_LENGTH_OFFSET);
        if (payloadLength < 0 || HEADER_SIZE + payloadLength > BUFFER_SIZE)
        {
            return null;
        }

        // 페이로드 읽기
        var isReadPayload = await ReadBytesAsync(buffer, payloadLength, HEADER_SIZE);
        if (!isReadPayload)
        {
            return null;
        }

        // 패킷 생성
        var packetId = BitConverter.ToInt32(buffer, HEADER_PACKET_ID_OFFSET);
        var payload = new ReadOnlyMemory<byte>(buffer, HEADER_SIZE, payloadLength);

        return _packetFactory.CreatePacket(packetId, payload);
    }

    private async Task<bool> ReadBytesAsync(byte[] buffer, int lengthToRead, int offset)
    {
        if (lengthToRead > buffer.Length)
        {
            return false;
        }

        // 남은 데이터를 모두 읽을 때까지 반복
        while (lengthToRead > 0)
        {
            var lengthReceived = await _client.ReceiveAsync(buffer, offset, lengthToRead);
            if (lengthReceived == 0)
            {
                return false;
            }

            offset += lengthReceived;
            lengthToRead -= lengthReceived;
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
            if (!_sendQueue.TryDequeue(out var packet))
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
                await SendPacketAsync(packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    private async Task SendPacketAsync(IPacket packet)
    {
        // TODO: 직렬화를 미리 끝내면 전송 간의 시간 차이가 줄어들어 성능 향상
        var payload = packet.Serialize();

        var packetIdLength = BitConverter.GetBytes((int)packet.PacketId);
        var payloadLength = BitConverter.GetBytes(payload.Length);

        var sendBuffer = new byte[HEADER_SIZE + payload.Length];
        Buffer.BlockCopy(packetIdLength, 0, sendBuffer, HEADER_PACKET_ID_OFFSET, sizeof(int));
        Buffer.BlockCopy(payloadLength, 0, sendBuffer, HEADER_LENGTH_OFFSET, sizeof(int));
        Buffer.BlockCopy(payload, 0, sendBuffer, HEADER_SIZE, payload.Length);

        await _client.SendAsync(sendBuffer);
    }


    public void Disconnect()
    {
        _client.Close();
    }
}