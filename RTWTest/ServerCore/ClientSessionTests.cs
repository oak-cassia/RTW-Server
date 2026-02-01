using System.Reflection;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Moq;
using NetworkDefinition.ErrorCode;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.ServerCore.implementation;
using RTWServer.ServerCore.Interface;

namespace RTWTest.ServerCore;

public class ClientSessionTests
{
    private class DummyClient : IClient
    {
        public Stream Stream { get; } = new MemoryStream();
        public bool IsConnected => true;
        public ValueTask<int> ReceiveAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken = default) => new(0);
        public void Close() { }
        public void Dispose() { }
    }

    [Test]
    public async Task ValidateAuthTokenAsync_WithValidToken_SetsPropertiesAndReturnsSuccess()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var client = new DummyClient();
        var packetHandler = new Mock<IPacketHandler>().Object;
        var packetSerializer = new Mock<IPacketSerializer>().Object;

        var session = new ClientSession(client, packetHandler, packetSerializer, loggerFactory, "session1");

        var (errorCode, playerId) = await session.ValidateAuthTokenAsync("token");

        Assert.That(errorCode, Is.EqualTo(RTWErrorCode.Success));
        Assert.That(playerId, Is.EqualTo("session1".GetHashCode()));
        Assert.That(session.AuthToken, Is.EqualTo("token"));
        Assert.That(session.IsAuthenticated, Is.True);
    }

    [Test]
    public async Task ValidateAuthTokenAsync_WithEmptyToken_ReturnsAuthenticationFailed()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var client = new DummyClient();
        var packetHandler = new Mock<IPacketHandler>().Object;
        var packetSerializer = new Mock<IPacketSerializer>().Object;

        var session = new ClientSession(client, packetHandler, packetSerializer, loggerFactory, "session1");

        var (errorCode, playerId) = await session.ValidateAuthTokenAsync(string.Empty);

        Assert.That(errorCode, Is.EqualTo(RTWErrorCode.AuthenticationFailed));
        Assert.That(playerId, Is.EqualTo(0));
        Assert.That(session.IsAuthenticated, Is.False);
    }

    private static ClientSession CreateSessionWithStrictSerializer(Mock<IPacketSerializer> serializerMock)
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var client = new DummyClient();
        var packetHandler = new Mock<IPacketHandler>().Object;
        return new ClientSession(client, packetHandler, serializerMock.Object, loggerFactory, "session-disconnected");
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException($"Field {fieldName} not found on {target.GetType().Name}");
        field.SetValue(target, value!);
    }

    private static void EnqueuePacket(object target, IPacket packet)
    {
        var field = target.GetType().GetField("_sendQueue", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("_sendQueue field not found");
        var enqueueMethod = field.FieldType.GetMethod("Enqueue", BindingFlags.Instance | BindingFlags.Public)
                           ?? throw new InvalidOperationException("Enqueue method not found on sendQueue");
        enqueueMethod.Invoke(field.GetValue(target), new object[] { packet });
    }

    private static Task InvokeFlush(object target)
    {
        var method = target.GetType().GetMethod("FlushSendQueueAsync", BindingFlags.Instance | BindingFlags.NonPublic)
                     ?? throw new InvalidOperationException("FlushSendQueueAsync not found");
        return (Task)method.Invoke(target, Array.Empty<object>())!;
    }

    private class DummyPacket : IPacket
    {
        public PacketId PacketId => PacketId.EchoMessage;
        public IMessage GetPayloadMessage() => null!;
        public int GetPayloadSize() => 0;
        public void WriteToBuffer(Span<byte> buffer) { }
    }

    [Test]
    public async Task SendAsync_WhenDisconnected_DoesNotInvokeSerializer()
    {
        var serializerMock = new Mock<IPacketSerializer>(MockBehavior.Strict);
        var session = CreateSessionWithStrictSerializer(serializerMock);

        SetPrivateField(session, "_connectionState", 0); // CONNECTION_STATE_DISCONNECTED

        await session.SendAsync(new DummyPacket());
    }

    [Test]
    public async Task FlushSendQueueAsync_ExitsCleanlyWhenDisconnected()
    {
        var serializerMock = new Mock<IPacketSerializer>(MockBehavior.Strict);
        var session = CreateSessionWithStrictSerializer(serializerMock);

        SetPrivateField(session, "_connectionState", 0); // CONNECTION_STATE_DISCONNECTED
        EnqueuePacket(session, new DummyPacket());

        await InvokeFlush(session);
    }
}
