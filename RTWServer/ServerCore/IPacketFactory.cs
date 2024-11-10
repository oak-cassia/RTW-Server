namespace RTWServer.ServerCore;

public interface IPacketFactory
{
    IPacket CreatePacket(int packetId, ReadOnlyMemory<byte> payload);
}