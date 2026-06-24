using Google.Protobuf;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Packet;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Packet;

public class GamePacketFactory : IPacketFactory
{
    public IPacket CreatePacket(int packetIdNum, ReadOnlySpan<byte> payloadBytes)
    {
        PacketId packetId = (PacketId)packetIdNum;

        // 와이어에서 들어오는 바이트는 신뢰할 수 없으므로 클라이언트가 보낼 수 있는 패킷만
        // 역직렬화한다. 서버 전용(S-접두: SAuthResult/SChat/...)과 내부 전용(ISessionClosed)은
        // 제외 — 내부 패킷은 서버가 직접 ProtoPacket으로 만들어 핸들러에 넘기므로 이 팩토리를
        // 거치지 않는다. 허용 외 PacketId는 예외로 던져 수신 루프가 세션을 종료시킨다.
        return packetId switch
        {
            PacketId.EchoMessage => new ProtoPacket(packetId, EchoMessage.Parser.ParseFrom(payloadBytes)),
            PacketId.CAuthToken => new ProtoPacket(packetId, CAuthToken.Parser.ParseFrom(payloadBytes)),
            PacketId.CChat => new ProtoPacket(packetId, CChat.Parser.ParseFrom(payloadBytes)),
            PacketId.CChatJoin => new ProtoPacket(packetId, CChatJoin.Parser.ParseFrom(payloadBytes)),
            PacketId.CChatLeave => new ProtoPacket(packetId, CChatLeave.Parser.ParseFrom(payloadBytes)),
            PacketId.CChatChat => new ProtoPacket(packetId, CChatChat.Parser.ParseFrom(payloadBytes)),
            _ => throw new ArgumentOutOfRangeException(nameof(packetId), packetId, "Packet id is not a client-sendable packet")
        };
    }

    public IPacket CreatePacket(int packetIdNum, IMessage message)
    {
        PacketId packetId = (PacketId)packetIdNum;
        return new ProtoPacket(packetId, message);
    }
}