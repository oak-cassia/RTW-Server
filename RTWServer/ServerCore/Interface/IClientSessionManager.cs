namespace RTWServer.ServerCore.Interface;

public interface IClientSessionManager
{
    void AddClientSession(IClientSession clientSession);
    
    void RemoveClientSession(string id);
    
    IClientSession? GetClientSession(string id);
    
    IEnumerable<IClientSession> GetAllClientSessions();
}