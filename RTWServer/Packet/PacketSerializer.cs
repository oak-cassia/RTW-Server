using System.IO.Pipelines;
using RTWServer.Enum;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Packet;

public class PacketSerializer(IPacketFactory packetFactory) : IPacketSerializer
{
    const int HEADER_SIZE = 8;
    const int PAYLOAD_SIZE_OFFSET = 4;

    public int GetHeaderSize()
    {
        return HEADER_SIZE;
    }

    public int GetPayloadSizeFromHeader(ReadOnlySpan<byte> header)
    {
        return BitConverter.ToInt32(header[PAYLOAD_SIZE_OFFSET..]);
    }

    public void SerializeToBuffer(IPacket packet, Span<byte> buffer)
    {
        int payloadSize = packet.GetPayloadSize();
        
        BitConverter.TryWriteBytes(buffer, (int)packet.PacketId);
        BitConverter.TryWriteBytes(buffer.Slice(PAYLOAD_SIZE_OFFSET), payloadSize);

        packet.WriteToBuffer(buffer.Slice(HEADER_SIZE, payloadSize));
    }

    public IPacket Deserialize(ReadOnlySpan<byte> buffer)
    {
        PacketId packetId = (PacketId)BitConverter.ToInt32(buffer.Slice(0, PAYLOAD_SIZE_OFFSET));

        int payloadSize = GetPayloadSizeFromHeader(buffer.Slice(0, HEADER_SIZE));
        ReadOnlySpan<byte> payload = buffer.Slice(HEADER_SIZE, payloadSize);

        return packetFactory.CreatePacket((int)packetId, payload);
    }
}