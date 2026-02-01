namespace RTWServer.ServerCore.Interface;

public interface IClientSessionManager
{
    // 새 클라이언트 연결을 처리하며 세션 생성과 시작을 포함
    Task HandleNewClientAsync(IClient client, CancellationToken token);

    Task RemoveClientSessionAsync(string id);

    IClientSession? GetClientSession(string id);

    IEnumerable<IClientSession> GetAllClientSessions();

    /// <summary>
    /// 지정한 클라이언트 세션에 대해 정상 종료를 요청
    /// </summary>
    Task InitiateClientDisconnectAsync(string sessionId, string reason);
}