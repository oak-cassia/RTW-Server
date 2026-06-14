namespace RTWServer.ServerCore.Interface;

public interface IClientSession : IAsyncDisposable
{
    string Id { get; } // 인증 후 플레이어 ID로도 사용되는 세션 ID
    int PlayerId { get; } // 프로세스 내에서 유일한 플레이어 ID (세션 생성 시 발급)
    long UserId { get; } // 인증 성공 시 확정되는 웹 서버 계정 ID (인증 신원). 미인증 시 0
    string? AuthToken { get; }
    bool IsAuthenticated { get; }

    Task StartSessionAsync(CancellationToken token);

    Task SendAsync(IPacket packet);

    // 웹 서버 세션(session_{userId})과 대조해 인증한다. 반환 튜플의 PlayerId는 인프로세스 라우팅용 식별자.
    Task<(NetworkDefinition.ErrorCode.RTWErrorCode ErrorCode, int PlayerId)> ValidateAuthTokenAsync(long userId, string authToken);

    /// <summary>
    /// 세션의 종료 절차를 시작하도록 요청합니다.
    /// </summary>
    /// <param name="reason">종료 요청 사유</param>
    Task RequestShutdownAsync(string reason);
}