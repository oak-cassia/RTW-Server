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
    
    public ClientSession(IClient client, IPacketHandler packetHandler, IPacketFactory packetFactory)
    {
        _client = client;
        _packetHandler = packetHandler;
        _packetFactory = packetFactory;
    }

    public async Task StartSessionAsync(CancellationToken token)
    {
        try
        {
            var buffer = new byte[BUFFER_SIZE];
            
            while (!token.IsCancellationRequested)
            {
                var isReadHeader = await FillAsync(buffer, HEADER_SIZE, 0);
                if (!isReadHeader)
                {
                    break;
                }
                
                var packetId = BitConverter.ToInt32(buffer, HEADER_PACKET_ID_OFFSET);
                var payloadLength = BitConverter.ToInt32(buffer, HEADER_LENGTH_OFFSET);
                if (payloadLength < 0 || payloadLength > BUFFER_SIZE - HEADER_SIZE)
                {
                    break;
                }
                
                var isReadPayload = await FillAsync(buffer, payloadLength, HEADER_SIZE);
                if (!isReadPayload)
                {
                    break;
                }
                
                var payload = new ReadOnlyMemory<byte>(buffer, HEADER_SIZE, payloadLength);
                var packet = _packetFactory.CreatePacket(packetId, payload);
                
                await _packetHandler.HandlePacketAsync(packet, _client);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    private async Task<bool> FillAsync(byte[] buffer, int size, int offset)
    {
        // 요청된 크기가 버퍼 크기를 초과하면 false 반환
        if (size > buffer.Length)
        {
            return false;
        }

        // 남은 데이터를 모두 읽을 때까지 반복
        while (size > 0)
        {
            var readLength = await _client.ReadAsync(buffer, offset, size);
            if (readLength == 0)
            {
                return false;
            }
            
            offset += readLength;
            size -= readLength;
        }

        return true;
    }
}