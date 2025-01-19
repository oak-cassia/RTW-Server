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

    public int GetPayloadSize(ReadOnlySpan<byte> header)
    {
        return BitConverter.ToInt32(header[PAYLOAD_SIZE_OFFSET..]);
    }

    public byte[] Serialize(IPacket packet)
    {
        var payloadSize = packet.GetPayloadSize();
        var buffer = new byte[HEADER_SIZE + payloadSize];

        BitConverter.TryWriteBytes(buffer.AsSpan(), (int)packet.PacketId);
        BitConverter.TryWriteBytes(buffer.AsSpan(PAYLOAD_SIZE_OFFSET), payloadSize);

        packet.WriteToBuffer(buffer.AsSpan(HEADER_SIZE, payloadSize));

        return buffer;
    }

    public IPacket Deserialize(ReadOnlySpan<byte> buffer)
    {
        var packetId = (PacketId)BitConverter.ToInt32(buffer.Slice(0, PAYLOAD_SIZE_OFFSET));

        var payloadSize = GetPayloadSize(buffer.Slice(0, HEADER_SIZE));
        ReadOnlySpan<byte> payload = buffer.Slice(HEADER_SIZE, payloadSize);

        return packetFactory.CreatePacket((int)packetId, payload);
    }
}