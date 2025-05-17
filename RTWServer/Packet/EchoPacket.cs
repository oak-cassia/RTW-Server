using RTWServer.Enum;

namespace RTWServer.Packet;

public class EchoPacket : BasePacket
{
    public EchoPacket(PacketId packetId, byte[] payload) 
        : base(packetId, payload)
    {
    }
    
    public EchoPacket(byte[] payload)
        : base(PacketId.EchoTest, payload)
    {
    }
}