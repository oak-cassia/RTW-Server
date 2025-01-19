using RTWServer.Enum;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Packet;

public class EchoPacket : IPacket
{
    public EchoPacket(PacketId packetId, byte[] payload)
    {
        PacketId = packetId;
        Payload = payload;
    }

    public PacketId PacketId { get; }
    public int GetPayloadSize()
    {
        return Payload.Length;
    }

    public void WriteToBuffer(Span<byte> buffer)
    {
        Payload.CopyTo(buffer);
    }

    public byte[] Payload { get; }

    public byte[] Serialize()
    {
        // 1) PacketId 4바이트
        var packetIdBytes = BitConverter.GetBytes((int)PacketId);

        // 2) 총 길이 = 헤더(8) + 페이로드 길이
        int totalLength = 8 + Payload.Length;
        var lengthBytes = BitConverter.GetBytes(totalLength);

        // 3) 최종 패킷: 8 + 페이로드 길이
        var packet = new byte[8 + Payload.Length];

        // 4) 헤더 복사 (packetId + totalLength)
        Array.Copy(packetIdBytes, 0, packet, 0, 4);
        Array.Copy(lengthBytes, 0, packet, 4, 4);

        // 5) 페이로드 복사
        Array.Copy(Payload, 0, packet, 8, Payload.Length);

        return packet;
    }
}