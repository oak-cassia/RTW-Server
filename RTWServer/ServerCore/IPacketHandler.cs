
namespace RTWServer.ServerCore;

public interface IPacketHandler
{
    Task HandlePacketAsync(IPacket packet, IClient stream);
}