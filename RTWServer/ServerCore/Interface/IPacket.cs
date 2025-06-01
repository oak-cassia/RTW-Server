using Google.Protobuf;
using RTW.NetworkDefinition.Proto.Packet;

namespace RTWServer.ServerCore.Interface;

public interface IPacket
{
    PacketId PacketId { get; }
    IMessage GetPayloadMessage();

    int GetPayloadSize();

    void WriteToBuffer(Span<byte> buffer);
}