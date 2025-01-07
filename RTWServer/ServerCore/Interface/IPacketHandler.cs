namespace RTWServer.ServerCore.Interface;

public interface IPacketHandler
{
    Task HandlePacketAsync(IPacket packet, IClient stream);
}