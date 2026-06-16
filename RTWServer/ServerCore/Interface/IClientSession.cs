namespace RTWServer.ServerCore.Interface;

public interface IClientSession : IAsyncDisposable
{
    string Id { get; } // 세션 ID (채팅 방 멤버십·라우팅 키)
    long UserId { get; } // 인증 성공 시 확정되는 웹 서버 계정 ID이자 플레이어 ID. 미인증 시 0
    string? AuthToken { get; }
    bool IsAuthenticated { get; }

    Task StartSessionAsync(CancellationToken token);

    Task SendAsync(IPacket packet);

    // 웹 서버 세션(session_{userId})과 대조해 인증한다. 성공 시 UserId가 확정된다.
    Task<NetworkDefinition.ErrorCode.RTWErrorCode> ValidateAuthTokenAsync(long userId, string authToken);

    /// <summary>
    /// 세션의 종료 절차를 시작하도록 요청합니다.
    /// </summary>
    /// <param name="reason">종료 요청 사유</param>
    Task RequestShutdownAsync(string reason);
}