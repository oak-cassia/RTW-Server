using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Packet;
using RTWServer.ServerCore.Interface;
using Google.Protobuf;

namespace RTWServer.Game.Packet;

public class GamePacketFactory : IPacketFactory
{
    public IPacket CreatePacket(int packetIdNum, ReadOnlySpan<byte> payloadBytes)
    {
        PacketId packetId = (PacketId)packetIdNum;

        return packetId switch
        {
            PacketId.EchoMessage => new ProtoPacket(packetId, EchoMessage.Parser.ParseFrom(payloadBytes)),
            PacketId.CAuthToken => new ProtoPacket(packetId, CAuthToken.Parser.ParseFrom(payloadBytes)),
            PacketId.SAuthResult => new ProtoPacket(packetId, SAuthResult.Parser.ParseFrom(payloadBytes)),
            _ => throw new ArgumentOutOfRangeException(nameof(packetId), packetId, null)
        };
    }

    public IPacket CreatePacket(int packetIdNum, IMessage message)
    {
        PacketId packetId = (PacketId)packetIdNum;
        return new ProtoPacket(packetId, message);
    }
}