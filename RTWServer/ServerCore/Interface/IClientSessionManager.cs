namespace RTWServer.ServerCore.Interface;

public interface IClientSessionManager
{
    void AddClientSession(IClientSession clientSession);
    
    void RemoveClientSession(IClientSession clientSession);
    
    IClientSession? GetClientSession(string id);
    
    IEnumerable<IClientSession> GetAllClientSessions();
}