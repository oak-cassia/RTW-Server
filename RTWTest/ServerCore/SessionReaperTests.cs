using Microsoft.Extensions.Logging;
using Moq;
using RTWServer.ServerCore.implementation;
using RTWServer.ServerCore.Interface;

namespace RTWTest.ServerCore;

// 단일 스윕 루프가 무인증·인증-데드라인 초과 세션만 정리하고 인증된 세션은 건드리지 않음을 검증한다.
[TestFixture]
public class SessionReaperTests
{
    private static Mock<IClientSession> Session(bool authenticated, DateTime connectedAtUtc)
    {
        var mock = new Mock<IClientSession>();
        mock.SetupGet(s => s.IsAuthenticated).Returns(authenticated);
        mock.SetupGet(s => s.ConnectedAtUtc).Returns(connectedAtUtc);
        mock.SetupGet(s => s.Id).Returns(Guid.NewGuid().ToString());
        mock.Setup(s => s.RequestShutdownAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        return mock;
    }

    private static SessionReaper CreateReaper(IEnumerable<IClientSession> sessions, TimeSpan authTimeout)
    {
        var manager = new Mock<IClientSessionManager>();
        manager.Setup(m => m.GetAllClientSessions()).Returns(sessions);
        var loggerFactory = LoggerFactory.Create(_ => { });
        return new SessionReaper(manager.Object, loggerFactory, authTimeout: authTimeout);
    }

    [Test]
    public async Task SweepAsync_UnauthenticatedPastDeadline_IsReaped()
    {
        var stale = Session(authenticated: false, DateTime.UtcNow.AddSeconds(-30));
        var reaper = CreateReaper(new[] { stale.Object }, TimeSpan.FromSeconds(10));

        await reaper.SweepAsync();

        stale.Verify(s => s.RequestShutdownAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task SweepAsync_UnauthenticatedWithinDeadline_IsNotReaped()
    {
        var fresh = Session(authenticated: false, DateTime.UtcNow.AddSeconds(-2));
        var reaper = CreateReaper(new[] { fresh.Object }, TimeSpan.FromSeconds(10));

        await reaper.SweepAsync();

        fresh.Verify(s => s.RequestShutdownAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task SweepAsync_AuthenticatedSession_IsNeverReaped()
    {
        // 인증된 연결은 오래됐어도 건드리지 않는다(유휴 타임아웃은 ping/pong 도입 시 별도).
        var authedOld = Session(authenticated: true, DateTime.UtcNow.AddHours(-1));
        var reaper = CreateReaper(new[] { authedOld.Object }, TimeSpan.FromSeconds(10));

        await reaper.SweepAsync();

        authedOld.Verify(s => s.RequestShutdownAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task SweepAsync_MixedSessions_ReapsOnlyStaleUnauthenticated()
    {
        var stale = Session(authenticated: false, DateTime.UtcNow.AddSeconds(-30));
        var fresh = Session(authenticated: false, DateTime.UtcNow.AddSeconds(-1));
        var authed = Session(authenticated: true, DateTime.UtcNow.AddHours(-1));
        var reaper = CreateReaper(new[] { stale.Object, fresh.Object, authed.Object }, TimeSpan.FromSeconds(10));

        await reaper.SweepAsync();

        stale.Verify(s => s.RequestShutdownAsync(It.IsAny<string>()), Times.Once);
        fresh.Verify(s => s.RequestShutdownAsync(It.IsAny<string>()), Times.Never);
        authed.Verify(s => s.RequestShutdownAsync(It.IsAny<string>()), Times.Never);
    }
}
