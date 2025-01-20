using RTWServer.Enum;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Packet;

public class EchoPacket : IPacket
{
    public PacketId PacketId { get; }
    public byte[] Payload { get; }

    public EchoPacket(PacketId packetId, byte[] payload)
    {
        PacketId = packetId;
        Payload = payload;
    }

    public int GetPayloadSize()
    {
        return Payload.Length;
    }

    public void WriteToBuffer(Span<byte> buffer)
    {
        Payload.CopyTo(buffer);
    }
}