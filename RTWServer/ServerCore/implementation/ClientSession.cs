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

    public string Id { get; private init; }

    public ClientSession(IClient client, IPacketHandler packetHandler, IPacketFactory packetFactory, string id)
    {
        _client = client;
        _packetHandler = packetHandler;
        _packetFactory = packetFactory;
        Id = id;
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

    public void Disconnect()
    {
        _client.Close();
    }
}