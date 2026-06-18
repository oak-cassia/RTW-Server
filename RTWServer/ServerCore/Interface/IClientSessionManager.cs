namespace RTWServer.ServerCore.Interface;

public interface IClientSessionManager
{
    // 새 클라이언트 연결을 처리하며 세션 생성과 시작을 포함
    Task HandleNewClientAsync(IClient client, CancellationToken token);

    Task RemoveClientSessionAsync(string id);

    IClientSession? GetClientSession(string id);

    // 인증된 userId로 현재 세션을 조회한다. 단일 세션 강제 덕에 userId당 최대 하나다.
    // 파티/재접속 슬롯 회수의 토대가 된다. 미인증·미접속이면 null.
    IClientSession? GetSessionByUserId(long userId);

    IEnumerable<IClientSession> GetAllClientSessions();

    /// <summary>
    /// 지정한 클라이언트 세션에 대해 정상 종료를 요청
    /// </summary>
    Task InitiateClientDisconnectAsync(string sessionId, string reason);
}