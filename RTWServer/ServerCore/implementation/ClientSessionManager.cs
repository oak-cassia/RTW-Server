using System.Collections.Concurrent;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class ClientSessionManager : IClientSessionManager
{
    private ConcurrentDictionary<string, IClientSession> _clientSessions = new();

    public void AddClientSession(IClientSession clientSession)
    {
        _clientSessions[clientSession.Id] = clientSession;
    }

    public void RemoveClientSession(string id)
    {
        _clientSessions.TryRemove(id, out _);
    }

    public IClientSession? GetClientSession(string id)
    {
        return _clientSessions.GetValueOrDefault(id);
    }

    public IEnumerable<IClientSession> GetAllClientSessions()
    {
        return _clientSessions.Values;
    }
}