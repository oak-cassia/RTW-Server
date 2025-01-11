using System.Collections.Concurrent;

namespace RTWServer.ServerCore.implementation;

public class ClientSessionManager
{
    private ConcurrentDictionary<string, ClientSession> _clientSessions = new ConcurrentDictionary<string, ClientSession>();
    
    public void AddClientSession(ClientSession clientSession)
    {
        _clientSessions[clientSession.Id] = clientSession;
    }
    
    public void RemoveClientSession(ClientSession clientSession)
    {
        _clientSessions.TryRemove(clientSession.Id, out _);
        
        clientSession.Disconnect();
    }
    
    public ClientSession? GetClientSession(string id)
    {
        return _clientSessions.GetValueOrDefault(id);
    }
    
    public IEnumerable<ClientSession> GetAllClientSessions()
    {
        return _clientSessions.Values;
    }
}