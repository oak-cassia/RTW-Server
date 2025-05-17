using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class ClientSessionManager : IClientSessionManager
{
    private ConcurrentDictionary<string, IClientSession> _clientSessions = new();
    private readonly ILoggerFactory _loggerFactory;

    public ClientSessionManager(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IClientSession CreateClientSession(IClient client, IPacketHandler packetHandler, IPacketSerializer packetSerializer, ILoggerFactory loggerFactoryForSession)
    {
        string sessionId = Guid.NewGuid().ToString();
        var session = new ClientSession(client, packetHandler, packetSerializer, this, loggerFactoryForSession, sessionId);
        AddClientSession(session);
        return session;
    }

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