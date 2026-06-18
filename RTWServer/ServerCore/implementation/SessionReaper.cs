using Microsoft.Extensions.Logging;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

/// <summary>
/// 세션당 타이머를 거는 대신, 단일 백그라운드 스윕 루프가 주기적으로 전체 세션을 훑어
/// 만료 세션을 정리한다. 현재는 무인증 연결의 인증-데드라인만 강제한다(연결만 해놓고
/// 인증하지 않는 연결이 자원을 점유하는 것을 막는다). 인증된 연결의 ping/pong 유휴
/// 타임아웃은 이번 범위 밖이며 게임 프로토콜과 함께 추가한다.
/// </summary>
public class SessionReaper
{
    private static readonly TimeSpan DefaultSweepInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DefaultAuthTimeout = TimeSpan.FromSeconds(10);

    private readonly IClientSessionManager _sessionManager;
    private readonly ILogger<SessionReaper> _logger;
    private readonly TimeSpan _sweepInterval;
    private readonly TimeSpan _authTimeout;

    public SessionReaper(
        IClientSessionManager sessionManager,
        ILoggerFactory loggerFactory,
        TimeSpan? sweepInterval = null,
        TimeSpan? authTimeout = null)
    {
        _sessionManager = sessionManager;
        _logger = loggerFactory.CreateLogger<SessionReaper>();
        _sweepInterval = sweepInterval ?? DefaultSweepInterval;
        _authTimeout = authTimeout ?? DefaultAuthTimeout;
    }

    public async Task RunAsync(CancellationToken token)
    {
        _logger.LogInformation(
            "Session reaper started (sweep {SweepSeconds}s, auth timeout {AuthSeconds}s)",
            _sweepInterval.TotalSeconds, _authTimeout.TotalSeconds);

        using var timer = new PeriodicTimer(_sweepInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                await SweepAsync();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Session reaper stopped via cancellation");
        }
    }

    // 한 번의 스윕. 단위 테스트에서 직접 호출할 수 있도록 루프와 분리했다.
    public async Task SweepAsync()
    {
        DateTime now = DateTime.UtcNow;

        foreach (IClientSession session in _sessionManager.GetAllClientSessions())
        {
            // 인증된 연결은 건드리지 않는다(유휴 타임아웃은 ping/pong 도입 시 별도 처리).
            if (session.IsAuthenticated)
            {
                continue;
            }

            TimeSpan elapsed = now - session.ConnectedAtUtc;
            if (elapsed > _authTimeout)
            {
                _logger.LogInformation(
                    "Reaping unauthenticated session {SessionId} (connected {ElapsedSeconds:F1}s ago)",
                    session.Id, elapsed.TotalSeconds);
                await session.RequestShutdownAsync("Authentication timeout");
            }
        }
    }
}
