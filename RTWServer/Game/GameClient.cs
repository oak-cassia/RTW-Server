using System.Net.Sockets;
using RTWServer.ServerCore;

namespace RTWServer.Game;

public class GameClient : IClient
{
    TcpClient _tcpClient;

    public GameClient(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
    }

    public async Task SendPacketAsync(PacketId packetId, byte[] payload)
    {
        var stream = GetStream();

        // 패킷 생성
        var header = new byte[8];
        var packetIdBytes = BitConverter.GetBytes((int)packetId);
        var lengthBytes = BitConverter.GetBytes(payload.Length + header.Length);

        Array.Copy(packetIdBytes, 0, header, 0, 4); // 패킷 ID 복사
        Array.Copy(lengthBytes, 0, header, 4, 4); // 패킷 길이 복사

        // 헤더와 페이로드 결합
        var packet = new byte[header.Length + payload.Length];
        Array.Copy(header, 0, packet, 0, header.Length);
        Array.Copy(payload, 0, packet, header.Length, payload.Length);

        // 패킷 전송
        await stream.WriteAsync(packet, 0, packet.Length);
    }

    public NetworkStream GetStream()
    {
        return _tcpClient.GetStream();
    }

    public void Close()
    {
        _tcpClient.Close();
    }
}