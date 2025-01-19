namespace RTWServer.ServerCore.Interface;

public interface IPacketSerializer
{
    int GetHeaderSize();
    int GetPayloadSize(ReadOnlySpan<byte> header);
    byte[] Serialize(IPacket packet);
    IPacket Deserialize(ReadOnlySpan<byte> buffer);
}