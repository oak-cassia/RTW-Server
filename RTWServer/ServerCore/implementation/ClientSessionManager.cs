using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RTWServer.Game.Chat;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class ClientSessionManager : IClientSessionManager
{
    private ConcurrentDictionary<string, IClientSession> _clientSessions = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPacketHandler _packetHandler;
    private readonly IPacketSerializer _packetSerializer;
    private readonly ILogger<ClientSessionManager> _logger;
    private readonly IChatHandler? _chatHandler;

    public ClientSessionManager(ILoggerFactory loggerFactory, IPacketHandler packetHandler, IPacketSerializer packetSerializer, IChatHandler? chatHandler = null)
    {
        _loggerFactory = loggerFactory;
        _packetHandler = packetHandler;
        _packetSerializer = packetSerializer;
        _chatHandler = chatHandler;
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

            // StartSessionAsync가 내부에서 예외를 처리하고 수명 주기를 관리
            await session.StartSessionAsync(token);
        }
        catch (Exception ex)
        {
            // CreateClientSession에서 실패했거나, 
            // StartSessionAsync 호출 '직전'에 예외가 발생한 경우 등 초기화 실패 처리
            string clientInfo = session?.Id ?? client.ToString() ?? "unknown";
            _logger.LogError(ex, "Failed to initialize or start session for client {ClientInfo}", clientInfo);

            // 세션이 생성되었지만 StartSessionAsync에 진입하지 못했거나
            // StartSessionAsync 내부 로직이 돌기 전에 예외가 터진 경우 안전하게 제거
            if (session != null)
            {
                RemoveClientSession(session.Id);
            }
        }
    }

    public void RemoveClientSession(string id)
    {
        _chatHandler?.CleanupSession(id);
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
            await session.RequestShutdownAsync(reason); // RequestShutdownAsync를 await 처리
        }
        else
        {
            _logger.LogWarning("Attempted to disconnect non-existent session {SessionId}.", sessionId);
        }
    }
}
