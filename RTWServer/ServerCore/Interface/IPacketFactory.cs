namespace RTWServer.ServerCore.Interface;

public interface IPacketFactory
{
    IPacket CreatePacket(int packetId, ReadOnlyMemory<byte> payload);
}