using Microsoft.Extensions.Logging;
using Moq;
using NetworkDefinition.ErrorCode;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Game.Chat;
using RTWServer.Game.Packet;
using RTWServer.Game.Player;
using RTWServer.Packet;
using RTWServer.ServerCore.Interface;

namespace RTWTest.Game;

// L2: EchoMessage·CChatLeave에도 다른 핸들러와 동일한 인증 게이트가 적용되는지 검증한다.
[TestFixture]
public class GamePacketHandlerTests
{
    private const string DefaultRoom = "global";

    private static GamePacketHandler CreateHandler(IChatService chatService)
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        return new GamePacketHandler(loggerFactory, chatService, DefaultRoom);
    }

    private static Mock<IClientSession> CreateSession(bool authenticated)
    {
        var session = new Mock<IClientSession>();
        session.SetupGet(s => s.Id).Returns("c1");
        session.SetupGet(s => s.IsAuthenticated).Returns(authenticated);
        session.Setup(s => s.SendAsync(It.IsAny<IPacket>())).Returns(Task.CompletedTask);
        return session;
    }

    [Test]
    public async Task Echo_WhenNotAuthenticated_IsNotEchoedBack()
    {
        var chatService = new Mock<IChatService>();
        var handler = CreateHandler(chatService.Object);
        var session = CreateSession(authenticated: false);
        var packet = new ProtoPacket(PacketId.EchoMessage, new EchoMessage { Message = "ping" });

        await handler.HandlePacketAsync(packet, session.Object);

        session.Verify(s => s.SendAsync(It.IsAny<IPacket>()), Times.Never);
    }

    [Test]
    public async Task Echo_WhenAuthenticated_IsEchoedBack()
    {
        var chatService = new Mock<IChatService>();
        var handler = CreateHandler(chatService.Object);
        var session = CreateSession(authenticated: true);
        var packet = new ProtoPacket(PacketId.EchoMessage, new EchoMessage { Message = "ping" });

        await handler.HandlePacketAsync(packet, session.Object);

        session.Verify(s => s.SendAsync(packet), Times.Once);
    }

    [Test]
    public async Task ChatLeave_WhenNotAuthenticated_RejectsWithoutTouchingChatService()
    {
        var chatService = new Mock<IChatService>();
        var handler = CreateHandler(chatService.Object);
        var session = CreateSession(authenticated: false);

        IPacket? sent = null;
        session.Setup(s => s.SendAsync(It.IsAny<IPacket>()))
            .Callback<IPacket>(p => sent = p)
            .Returns(Task.CompletedTask);

        var packet = new ProtoPacket(PacketId.CChatLeave, new CChatLeave { RoomId = "r1" });

        await handler.HandlePacketAsync(packet, session.Object);

        chatService.Verify(c => c.LeaveRoomAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.That(sent, Is.Not.Null);
        Assert.That(sent!.PacketId, Is.EqualTo(PacketId.SChatLeaveResult));
        var payload = (SChatLeaveResult)((ProtoPacket)sent).GetPayloadMessage();
        Assert.That((RTWErrorCode)payload.ErrorCode, Is.EqualTo(RTWErrorCode.AuthenticationFailed));
    }

    [Test]
    public async Task ChatLeave_WhenAuthenticated_DelegatesToChatService()
    {
        var chatService = new Mock<IChatService>();
        chatService.Setup(c => c.LeaveRoomAsync("r1", "c1")).ReturnsAsync(RTWErrorCode.Success);
        var handler = CreateHandler(chatService.Object);
        var session = CreateSession(authenticated: true);
        var packet = new ProtoPacket(PacketId.CChatLeave, new CChatLeave { RoomId = "r1" });

        await handler.HandlePacketAsync(packet, session.Object);

        chatService.Verify(c => c.LeaveRoomAsync("r1", "c1"), Times.Once);
    }

    [Test]
    public async Task AuthToken_PassesProtoUserIdToSession_AndRepliesWithResult()
    {
        var chatService = new Mock<IChatService>();
        chatService.Setup(c => c.JoinRoomAsync(It.IsAny<string>(), It.IsAny<IPlayer>())).ReturnsAsync(RTWErrorCode.Success);
        var handler = CreateHandler(chatService.Object);

        var session = CreateSession(authenticated: false);
        session.Setup(s => s.ValidateAuthTokenAsync(7L, "tok")).ReturnsAsync(RTWErrorCode.Success);
        // 인증 성공 시 세션의 UserId가 확정되며, 이 값이 곧 SAuthResult.playerId로 회신된다
        session.SetupGet(s => s.UserId).Returns(7L);

        IPacket? sent = null;
        session.Setup(s => s.SendAsync(It.IsAny<IPacket>()))
            .Callback<IPacket>(p => sent = p)
            .Returns(Task.CompletedTask);

        var packet = new ProtoPacket(PacketId.CAuthToken, new CAuthToken { UserId = 7, AuthToken = "tok" });

        await handler.HandlePacketAsync(packet, session.Object);

        // 핸들러가 클라이언트 본문의 userId를 그대로 세션 검증에 전달해야 한다
        session.Verify(s => s.ValidateAuthTokenAsync(7L, "tok"), Times.Once);
        Assert.That(sent, Is.Not.Null);
        Assert.That(sent!.PacketId, Is.EqualTo(PacketId.SAuthResult));
        var payload = (SAuthResult)((ProtoPacket)sent).GetPayloadMessage();
        Assert.That((RTWErrorCode)payload.ErrorCode, Is.EqualTo(RTWErrorCode.Success));
        Assert.That(payload.PlayerId, Is.EqualTo(7L));
    }
}
