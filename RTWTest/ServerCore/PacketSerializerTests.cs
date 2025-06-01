using RTWServer.Game;
using RTWServer.Packet;
using RTWServer.ServerCore.Interface;
using RTW.NetworkDefinition.Proto.Packet;

namespace RTWTest.ServerCore;

public class PacketSerializerTests
{
    [Test]
    public void SerializeAndDeserialize_ProtoPacket_RoundTripsCorrectly()
    {
        IPacketFactory factory = new GamePacketFactory();
        var serializer = new PacketSerializer(factory);
        var echoMessage = new EchoMessage { Message = "hello" };
        var original = new ProtoPacket(PacketId.EchoMessage, echoMessage);
        Span<byte> buffer = stackalloc byte[serializer.GetHeaderSize() + original.GetPayloadSize()];

        serializer.SerializeToBuffer(original, buffer);
        var result = serializer.Deserialize(buffer);

        Assert.That(result, Is.TypeOf<ProtoPacket>());
        Assert.That(result.GetPayloadSize(), Is.EqualTo(original.GetPayloadSize()));
    }
}
