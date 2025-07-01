using Google.Protobuf;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Packet;

public class ProtoPacket(PacketId packetId, IMessage payloadMessage) : IPacket
{
    private IMessage PayloadMessage { get; } = payloadMessage;
    public PacketId PacketId { get; } = packetId;

    // 생성자에서 IMessage를 받도록 변경

    public virtual int GetPayloadSize()
    {
        return PayloadMessage.CalculateSize();
    }

    public virtual void WriteToBuffer(Span<byte> buffer)
    {
        PayloadMessage.WriteTo(buffer);
    }

    // IPacket 인터페이스에 추가된 메서드 구현
    public IMessage GetPayloadMessage()
    {
        return PayloadMessage;
    }
}