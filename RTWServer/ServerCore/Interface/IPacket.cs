using RTWServer.Enum;

namespace RTWServer.ServerCore.Interface;

public interface IPacket
{
    PacketId PacketId { get; }

    int GetPayloadSize();

    void WriteToBuffer(Span<byte> buffer);
}