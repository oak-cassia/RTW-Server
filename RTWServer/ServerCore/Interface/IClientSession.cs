namespace RTWServer.ServerCore.Interface;

public interface IClientSession : IAsyncDisposable
{
    string Id { get; } // 인증 후 플레이어 ID로도 사용되는 세션 ID
    int PlayerId { get; } // 프로세스 내에서 유일한 플레이어 ID (세션 생성 시 발급)
    string? AuthToken { get; }
    bool IsAuthenticated { get; }

    Task StartSessionAsync(CancellationToken token);

    Task SendAsync(IPacket packet);

    // 반환 튜플의 PlayerId는 인증된 세션 ID(또는 필요 시 매핑된 값)를 의미합니다.
    Task<(NetworkDefinition.ErrorCode.RTWErrorCode ErrorCode, int PlayerId)> ValidateAuthTokenAsync(string authToken);

    /// <summary>
    /// 세션의 종료 절차를 시작하도록 요청합니다.
    /// </summary>
    /// <param name="reason">종료 요청 사유</param>
    Task RequestShutdownAsync(string reason);
}