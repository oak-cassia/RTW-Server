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
        var session = new ClientSession(client, _packetHandler, _packetSerializer, this, loggerFactoryForSession, sessionId);
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

            // StartSessionAsync will now handle its own lifecycle, including removal on completion/error.
            await session.StartSessionAsync(token); 
        }
        catch (OperationCanceledException)
        {
            // This catch block might only be hit if CreateClientSession itself is cancelled 
            // or if StartSessionAsync rethrows OperationCanceledException before its own finally block runs.
            // ClientSession.StartSessionAsync should handle its own cancellation logging and cleanup.
            string sessionId = session?.Id ?? client.ToString() ?? "unknown"; // Use client.ToString() as a fallback if session is null
            _logger.LogInformation("Handling of new client {SessionId} was cancelled.", sessionId);
            // If session was created but StartSessionAsync didn't run or complete its finally block, 
            // ensure it's removed. However, primary removal responsibility is now with ClientSession.
            if (session != null && _clientSessions.ContainsKey(session.Id)) 
            {
                // This is a fallback, ideally ClientSession.StartSessionAsync().finally handles this.
                RemoveClientSession(session.Id); 
            }
        }
        catch (Exception ex) // Catch all other exceptions during session creation or initial handling
        {
            string sessionId = session?.Id ?? client.ToString() ?? "unknown";
            _logger.LogError(ex, "Unexpected error while handling new client {SessionId}", sessionId);
            if (session != null && _clientSessions.ContainsKey(session.Id))
            {
                // Fallback removal
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
            if (session != null)
            {
                _logger.LogInformation("Initiating disconnect for session {SessionId} due to: {Reason}", sessionId, reason);
                await session.RequestShutdownAsync(reason); // Changed to await session.RequestShutdownAsync
            }
            else
            {
                // This case should ideally not happen if TryGetValue returns true and session is null.
                // Logging it defensively.
                _logger.LogWarning("Found a null session entry for ID {SessionId} while trying to disconnect.", sessionId);
                // Attempt to remove it anyway, as it's an invalid state.
                RemoveClientSession(sessionId);
            }
        }
        else
        {
            _logger.LogWarning("Attempted to disconnect non-existent session {SessionId}.", sessionId);
        }
    }
}