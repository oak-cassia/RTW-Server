using System.Net.Sockets;
using RTWServer.Enum;

namespace RTWServer.ServerCore;

public interface IClient
{
    Task SendPacketAsync(PacketId packetId, byte[] payload);
    NetworkStream GetStream();
    void Close();
}