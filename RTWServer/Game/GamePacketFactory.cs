using RTWServer.Enum;
using RTWServer.Packet;
using RTWServer.Packet.System;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game;

public class GamePacketFactory : IPacketFactory
{
    public IPacket CreatePacket(int packetId, ReadOnlySpan<byte> payload)
    {
        PacketId castedPacketId = (PacketId)packetId;
        return castedPacketId switch
        {
            PacketId.EchoTest => new EchoPacket(castedPacketId, payload.ToArray()),
            PacketId.CAuthToken => new CAuthToken(castedPacketId, payload.ToArray()),
            PacketId.SAuthResult => new SAuthResult(castedPacketId, payload.ToArray()),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}