using RTWServer.Enum;
using RTWServer.ServerCore;

namespace RTWServer.Packet;

public class EchoPacket : IPacket
{
    public EchoPacket(PacketId packetId, byte[] payload)
    {
        PacketId = packetId;
        Payload = payload;
    }

    public PacketId PacketId { get; }
    public byte[] Payload { get; }

    public byte[] Serialize()
    {
        return Payload;
    }
}