using Google.Protobuf;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Game;
using RTWServer.Packet;

namespace RTWTest.Game;

[TestFixture]
public class GamePacketFactoryTests
{
    private GamePacketFactory _packetFactory;
    private PacketSerializer _packetSerializer;

    [SetUp]
    public void SetUp()
    {
        _packetFactory = new GamePacketFactory();
        _packetSerializer = new PacketSerializer(_packetFactory);
    }

    [Test]
    public void CreatePacket_EchoMessage_CreatesValidPacket()
    {
        // Arrange
        var originalMessage = new EchoMessage { Message = "Hello from unit test!" };
        var packetBytes = originalMessage.ToByteArray();

        // Act
        var packet = _packetFactory.CreatePacket((int)PacketId.EchoMessage, packetBytes);

        // Assert
        Assert.That(packet, Is.Not.Null);
        Assert.That(packet, Is.TypeOf<ProtoPacket>());
        Assert.That(packet.PacketId, Is.EqualTo(PacketId.EchoMessage));

        var protoPacket = (ProtoPacket)packet;
        var deserializedMessage = (EchoMessage)protoPacket.GetPayloadMessage();
        Assert.That(deserializedMessage.Message, Is.EqualTo(originalMessage.Message));
    }

    [Test]
    public void CreatePacket_CAuthToken_CreatesValidPacket()
    {
        // Arrange
        var originalToken = new CAuthToken { AuthToken = "test-unit-token-12345" };
        var packetBytes = originalToken.ToByteArray();

        // Act
        var packet = _packetFactory.CreatePacket((int)PacketId.CAuthToken, packetBytes);

        // Assert
        Assert.That(packet, Is.Not.Null);
        Assert.That(packet, Is.TypeOf<ProtoPacket>());
        Assert.That(packet.PacketId, Is.EqualTo(PacketId.CAuthToken));

        var protoPacket = (ProtoPacket)packet;
        var deserializedToken = (CAuthToken)protoPacket.GetPayloadMessage();
        Assert.That(deserializedToken.AuthToken, Is.EqualTo(originalToken.AuthToken));
    }

    [Test]
    public void CreatePacket_SAuthResult_CreatesValidPacket()
    {
        // Arrange
        var originalResult = new SAuthResult { PlayerId = 98765, ErrorCode = 0 };
        var packetBytes = originalResult.ToByteArray();

        // Act
        var packet = _packetFactory.CreatePacket((int)PacketId.SAuthResult, packetBytes);

        // Assert
        Assert.That(packet, Is.Not.Null);
        Assert.That(packet, Is.TypeOf<ProtoPacket>());
        Assert.That(packet.PacketId, Is.EqualTo(PacketId.SAuthResult));

        var protoPacket = (ProtoPacket)packet;
        var deserializedResult = (SAuthResult)protoPacket.GetPayloadMessage();
        Assert.That(deserializedResult.PlayerId, Is.EqualTo(originalResult.PlayerId));
        Assert.That(deserializedResult.ErrorCode, Is.EqualTo(originalResult.ErrorCode));
    }

    [Test]
    public void CreatePacket_UnknownPacketId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidPacketId = 12345; // Use a clearly invalid packet ID
        var emptyBytes = System.Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            _packetFactory.CreatePacket(invalidPacketId, emptyBytes));
    }

    [Test]
    public void SerializationRoundTrip_EchoMessage_PreservesData()
    {
        // Arrange
        var originalPacket = new ProtoPacket(PacketId.EchoMessage, new EchoMessage { Message = "Round trip test" });

        // Act
        var totalSize = _packetSerializer.GetHeaderSize() + originalPacket.GetPayloadSize();
        var buffer = new byte[totalSize];
        _packetSerializer.SerializeToBuffer(originalPacket, buffer);
        var deserializedPacket = _packetSerializer.Deserialize(buffer);

        // Assert
        Assert.That(deserializedPacket.PacketId, Is.EqualTo(originalPacket.PacketId));
        Assert.That(deserializedPacket, Is.TypeOf<ProtoPacket>());
        
        var originalMessage = (EchoMessage)originalPacket.GetPayloadMessage();
        var deserializedMessage = (EchoMessage)((ProtoPacket)deserializedPacket).GetPayloadMessage();
        Assert.That(deserializedMessage.Message, Is.EqualTo(originalMessage.Message));
    }

    [Test]
    public void SerializationRoundTrip_CAuthToken_PreservesData()
    {
        // Arrange
        var originalPacket = new ProtoPacket(PacketId.CAuthToken, new CAuthToken { AuthToken = "round-trip-token" });

        // Act
        var totalSize = _packetSerializer.GetHeaderSize() + originalPacket.GetPayloadSize();
        var buffer = new byte[totalSize];
        _packetSerializer.SerializeToBuffer(originalPacket, buffer);
        var deserializedPacket = _packetSerializer.Deserialize(buffer);

        // Assert
        Assert.That(deserializedPacket.PacketId, Is.EqualTo(originalPacket.PacketId));
        Assert.That(deserializedPacket, Is.TypeOf<ProtoPacket>());
        
        var originalToken = (CAuthToken)originalPacket.GetPayloadMessage();
        var deserializedToken = (CAuthToken)((ProtoPacket)deserializedPacket).GetPayloadMessage();
        Assert.That(deserializedToken.AuthToken, Is.EqualTo(originalToken.AuthToken));
    }

    [Test]
    public void SerializationRoundTrip_SAuthResult_PreservesData()
    {
        // Arrange
        var originalPacket = new ProtoPacket(PacketId.SAuthResult, new SAuthResult { PlayerId = 11111, ErrorCode = 200 });

        // Act
        var totalSize = _packetSerializer.GetHeaderSize() + originalPacket.GetPayloadSize();
        var buffer = new byte[totalSize];
        _packetSerializer.SerializeToBuffer(originalPacket, buffer);
        var deserializedPacket = _packetSerializer.Deserialize(buffer);

        // Assert
        Assert.That(deserializedPacket.PacketId, Is.EqualTo(originalPacket.PacketId));
        Assert.That(deserializedPacket, Is.TypeOf<ProtoPacket>());
        
        var originalResult = (SAuthResult)originalPacket.GetPayloadMessage();
        var deserializedResult = (SAuthResult)((ProtoPacket)deserializedPacket).GetPayloadMessage();
        Assert.That(deserializedResult.PlayerId, Is.EqualTo(originalResult.PlayerId));
        Assert.That(deserializedResult.ErrorCode, Is.EqualTo(originalResult.ErrorCode));
    }

    [Test]
    public void SerializationRoundTrip_MultiplePacketTypes_AllSucceed()
    {
        // Arrange
        var testCases = new[]
        {
            new ProtoPacket(PacketId.EchoMessage, new EchoMessage { Message = "Multi test 1" }),
            new ProtoPacket(PacketId.CAuthToken, new CAuthToken { AuthToken = "multi-test-token" }),
            new ProtoPacket(PacketId.SAuthResult, new SAuthResult { PlayerId = 22222, ErrorCode = 100 })
        };

        // Act & Assert
        foreach (var originalPacket in testCases)
        {
            var totalSize = _packetSerializer.GetHeaderSize() + originalPacket.GetPayloadSize();
            var buffer = new byte[totalSize];
            
            _packetSerializer.SerializeToBuffer(originalPacket, buffer);
            var deserializedPacket = _packetSerializer.Deserialize(buffer);
            
            Assert.That(deserializedPacket.PacketId, Is.EqualTo(originalPacket.PacketId), 
                $"Packet ID mismatch for {originalPacket.PacketId}");
            Assert.That(totalSize, Is.GreaterThan(_packetSerializer.GetHeaderSize()), 
                $"Buffer size should be larger than header for {originalPacket.PacketId}");
        }
    }
}
