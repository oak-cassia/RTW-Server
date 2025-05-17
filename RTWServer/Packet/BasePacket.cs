using RTWServer.Enum;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Packet;

/// <summary>
/// Base implementation of IPacket that handles common packet functionality
/// </summary>
public abstract class BasePacket : IPacket
{
    public PacketId PacketId { get; }
    protected byte[] Payload { get; }

    protected BasePacket(PacketId packetId, byte[] payload)
    {
        PacketId = packetId;
        Payload = payload;
    }

    public virtual int GetPayloadSize()
    {
        return Payload.Length;
    }

    public virtual void WriteToBuffer(Span<byte> buffer)
    {
        Payload.CopyTo(buffer);
    }
}
