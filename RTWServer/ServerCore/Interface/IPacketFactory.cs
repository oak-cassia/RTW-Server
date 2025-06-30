using Google.Protobuf;

namespace RTWServer.ServerCore.Interface;

public interface IPacketFactory
{
    IPacket CreatePacket(int packetId, ReadOnlySpan<byte> payload);
    IPacket CreatePacket(int packetId, IMessage message);
}