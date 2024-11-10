using RTWServer.Enum;

namespace RTWServer.ServerCore;

public interface IPacket
{
    PacketId PacketId { get; }
    
    byte[] Serialize();
}