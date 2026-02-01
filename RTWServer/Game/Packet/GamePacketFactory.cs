using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Packet;
using RTWServer.ServerCore.Interface;

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
            PacketId.CChat => new ProtoPacket(packetId, CChat.Parser.ParseFrom(payloadBytes)),
            PacketId.CChatJoin => new ProtoPacket(packetId, CChatJoin.Parser.ParseFrom(payloadBytes)),
            PacketId.CChatLeave => new ProtoPacket(packetId, CChatLeave.Parser.ParseFrom(payloadBytes)),
            PacketId.CChatChat => new ProtoPacket(packetId, CChatChat.Parser.ParseFrom(payloadBytes)),
            PacketId.SAuthResult => new ProtoPacket(packetId, SAuthResult.Parser.ParseFrom(payloadBytes)),
            PacketId.SChat => new ProtoPacket(packetId, SChat.Parser.ParseFrom(payloadBytes)),
            PacketId.SChatJoinResult => new ProtoPacket(packetId, SChatJoinResult.Parser.ParseFrom(payloadBytes)),
            PacketId.SChatLeaveResult => new ProtoPacket(packetId, SChatLeaveResult.Parser.ParseFrom(payloadBytes)),
            PacketId.ISessionClosed => new ProtoPacket(packetId, ISessionClosed.Parser.ParseFrom(payloadBytes)),
            _ => throw new ArgumentOutOfRangeException(nameof(packetId), packetId, null)
        };
    }
}