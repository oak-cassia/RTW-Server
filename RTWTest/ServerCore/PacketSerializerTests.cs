using RTWServer.Game;
using RTWServer.Packet;
using RTWServer.ServerCore.Interface;

namespace RTWTest.ServerCore;

public class PacketSerializerTests
{
    [Test]
    public void SerializeAndDeserialize_EchoPacket_RoundTripsCorrectly()
    {
        IPacketFactory factory = new GamePacketFactory();
        var serializer = new PacketSerializer(factory);
        var original = new EchoPacket("hello"u8.ToArray());
        Span<byte> buffer = stackalloc byte[serializer.GetHeaderSize() + original.GetPayloadSize()];

        serializer.SerializeToBuffer(original, buffer);
        var result = serializer.Deserialize(buffer);

        Assert.That(result, Is.TypeOf<EchoPacket>());
        Assert.That(((EchoPacket)result).GetPayloadSize(), Is.EqualTo(original.GetPayloadSize()));
    }
}
