using Microsoft.Extensions.Logging;
using Moq;
using RTWServer.ServerCore.implementation;
using RTWServer.ServerCore.Interface;
using NetworkDefinition.ErrorCode;

namespace RTWTest.ServerCore;

public class ClientSessionTests
{
    private class DummyClient : IClient
    {
        public Stream Stream { get; } = new MemoryStream();
        public bool IsConnected => true;
        public ValueTask<int> ReceiveAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken = default) => new ValueTask<int>(0);
        public void Close() { }
    }

    [Test]
    public async Task ValidateAuthTokenAsync_WithValidToken_SetsPropertiesAndReturnsSuccess()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var client = new DummyClient();
        var packetHandler = new Mock<IPacketHandler>().Object;
        var packetSerializer = new Mock<IPacketSerializer>().Object;
        var sessionManager = new Mock<IClientSessionManager>().Object;

        var session = new ClientSession(client, packetHandler, packetSerializer, sessionManager, loggerFactory, "session1");

        var (errorCode, playerId) = await session.ValidateAuthTokenAsync("token");

        Assert.That(errorCode, Is.EqualTo(RTWErrorCode.Success));
        Assert.That(playerId, Is.EqualTo(Math.Abs("session1".GetHashCode())));
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
        var sessionManager = new Mock<IClientSessionManager>().Object;

        var session = new ClientSession(client, packetHandler, packetSerializer, sessionManager, loggerFactory, "session1");

        var (errorCode, playerId) = await session.ValidateAuthTokenAsync(string.Empty);

        Assert.That(errorCode, Is.EqualTo(RTWErrorCode.AuthenticationFailed));
        Assert.That(playerId, Is.EqualTo(0));
        Assert.That(session.IsAuthenticated, Is.False);
    }
}
