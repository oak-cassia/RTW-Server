
namespace RTWServer.ServerCore;

public interface IPacketHandler
{
    Task HandlePacketAsync(int packetId, byte[] payload, IClient stream);
}