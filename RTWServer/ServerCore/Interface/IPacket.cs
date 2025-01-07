using RTWServer.Enum;

namespace RTWServer.ServerCore.Interface;

public interface IPacket
{
    PacketId PacketId { get; }
    
    byte[] Serialize();
}