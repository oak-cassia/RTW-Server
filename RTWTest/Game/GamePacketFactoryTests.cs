using Google.Protobuf;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Game.Packet;
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

    // 신뢰 경계: 서버 전용(S-접두)·내부 전용(ISessionClosed) 패킷은 와이어에서 들어와도
    // 역직렬화하지 않는다. 클라이언트가 이런 PacketId를 주입하면 세션이 종료되어야 한다.
    [TestCase(PacketId.SAuthResult)]
    [TestCase(PacketId.SChat)]
    [TestCase(PacketId.SChatJoinResult)]
    [TestCase(PacketId.SChatLeaveResult)]
    [TestCase(PacketId.ISessionClosed)]
    public void CreatePacket_ServerOnlyOrInternalPacket_Throws(PacketId packetId)
    {
        var emptyBytes = System.Array.Empty<byte>();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _packetFactory.CreatePacket((int)packetId, emptyBytes));
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
    public void Deserialize_ServerOnlyPacketFromWire_Throws()
    {
        // 서버는 SAuthResult를 직렬화해 클라로 보낼 수 있지만(아웃바운드), 같은 바이트가
        // 와이어로 되돌아오면(인바운드) 역직렬화를 거부해야 한다 — 신뢰 경계.
        var serverPacket = new ProtoPacket(PacketId.SAuthResult, new SAuthResult { PlayerId = 11111, ErrorCode = 200 });
        var totalSize = _packetSerializer.GetHeaderSize() + serverPacket.GetPayloadSize();
        var buffer = new byte[totalSize];
        _packetSerializer.SerializeToBuffer(serverPacket, buffer);

        Assert.Throws<ArgumentOutOfRangeException>(() => _packetSerializer.Deserialize(buffer));
    }

    [Test]
    public void SerializationRoundTrip_MultiplePacketTypes_AllSucceed()
    {
        // Arrange
        // 클라이언트가 보낼 수 있는 패킷만 라운드트립 대상이다(SAuthResult 같은 서버 전용은 제외).
        var testCases = new[]
        {
            new ProtoPacket(PacketId.EchoMessage, new EchoMessage { Message = "Multi test 1" }),
            new ProtoPacket(PacketId.CAuthToken, new CAuthToken { AuthToken = "multi-test-token" }),
            new ProtoPacket(PacketId.CChatChat, new CChatChat { Message = "multi test chat" })
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
