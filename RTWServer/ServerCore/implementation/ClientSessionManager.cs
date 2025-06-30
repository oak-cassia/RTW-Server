using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class ClientSessionManager : IClientSessionManager
{
    private ConcurrentDictionary<string, IClientSession> _clientSessions = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPacketHandler _packetHandler;
    private readonly IPacketSerializer _packetSerializer;
    private readonly ILogger<ClientSessionManager> _logger;

    public ClientSessionManager(ILoggerFactory loggerFactory, IPacketHandler packetHandler, IPacketSerializer packetSerializer)
    {
        _loggerFactory = loggerFactory;
        _packetHandler = packetHandler;
        _packetSerializer = packetSerializer;
        _logger = _loggerFactory.CreateLogger<ClientSessionManager>();
    }

    private IClientSession CreateClientSession(IClient client, ILoggerFactory loggerFactoryForSession)
    {
        string sessionId = Guid.NewGuid().ToString();
        var session = new ClientSession(client, _packetHandler, _packetSerializer, loggerFactoryForSession, sessionId);
        _clientSessions[session.Id] = session;
        return session;
    }

    public async Task HandleNewClientAsync(IClient client, CancellationToken token)
    {
        _logger.LogDebug("Attempting to create and start new session for client");
        IClientSession? session = null;

        try
        {
            session = CreateClientSession(client, _loggerFactory);
            _logger.LogDebug("Session {SessionId} created and added to session manager", session.Id);

            await session.StartSessionAsync(token);
        }
        catch (OperationCanceledException)
        {
            string sessionId = session?.Id ?? client.ToString() ?? "unknown";
            if (token.IsCancellationRequested)
            {
                _logger.LogInformation("Handling of new client {SessionId} was cancelled due to server shutdown.", sessionId);
            }
            else
            {
                _logger.LogInformation("Handling of new client {SessionId} was cancelled.", sessionId);
            }
        }
        catch (IOException ex)
        {
            string sessionId = session?.Id ?? client.ToString() ?? "unknown";
            _logger.LogWarning(ex, "Network error while handling client {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            string sessionId = session?.Id ?? client.ToString() ?? "unknown";
            _logger.LogError(ex, "Unexpected error while handling new client {SessionId}", sessionId);
        }
        finally
        {
            // 세션 정리는 항상 Manager에서 담당
            if (session != null)
            {
                RemoveClientSession(session.Id);
            }
        }
    }

    public void RemoveClientSession(string id)
    {
        if (_clientSessions.TryRemove(id, out _))
        {
            _logger.LogInformation("Client session {SessionId} removed.", id);
        }
        else
        {
            _logger.LogWarning("Attempted to remove non-existent session {SessionId}.", id);
        }
    }

    public IClientSession? GetClientSession(string id)
    {
        return _clientSessions.GetValueOrDefault(id);
    }

    public IEnumerable<IClientSession> GetAllClientSessions()
    {
        return _clientSessions.Values;
    }

    public async Task InitiateClientDisconnectAsync(string sessionId, string reason)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            _logger.LogWarning("Attempted to disconnect a session with null or empty ID.");
            return;
        }

        if (_clientSessions.TryGetValue(sessionId, out IClientSession? session))
        {
            _logger.LogInformation("Initiating disconnect for session {SessionId} due to: {Reason}", sessionId, reason);
            await session.RequestShutdownAsync(reason);
        }
        else
        {
            _logger.LogWarning("Attempted to disconnect non-existent session {SessionId}.", sessionId);
        }
    }
}