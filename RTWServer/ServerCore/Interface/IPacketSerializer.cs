namespace RTWServer.ServerCore.Interface;

public interface IPacketSerializer
{
    int GetHeaderSize();
    int GetPayloadSizeFromHeader(ReadOnlySpan<byte> header);

    void SerializeToBuffer(IPacket packet, Span<byte> buffer);
    IPacket Deserialize(ReadOnlySpan<byte> buffer);
}