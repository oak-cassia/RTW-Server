using RTWServer.ServerCore.implementation;

namespace RTWServer.ServerCore.Interface;

public interface IPacketHandler
{
    Task HandlePacketAsync(IPacket packet, ClientSession clientSession);
}