using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Packet;
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

            // StartSessionAsync가 내부에서 예외를 처리하고 수명 주기를 관리하며,
            // 종료 시 (정상 또는 예외) finally 블록에서 세션이 제거됩니다.
            await session.StartSessionAsync(token);
        }
        catch (Exception ex)
        {
            string clientInfo = session?.Id ?? client.ToString() ?? "unknown";
            _logger.LogError(ex, "Exception occurred during session execution for client {ClientInfo}", clientInfo);
        }
        finally
        {
            if (session != null)
            {
                await RemoveClientSessionAsync(session.Id);
                await session.DisposeAsync();
            }
        }
    }

    public async Task RemoveClientSessionAsync(string id)
    {
        if (_clientSessions.TryRemove(id, out var session))
        {
            _logger.LogDebug("Client session {SessionId} removed from manager, sending internal cleanup packet.", id);

            // 세션 종료를 알리는 내부 패킷 생성 및 처리
            try
            {
                var cleanupPacket = new ProtoPacket(PacketId.ISessionClosed, new ISessionClosed());
                await _packetHandler.HandlePacketAsync(cleanupPacket, session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cleanup handler failed for session {SessionId}", id);
            }
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
            return;
        }

        if (_clientSessions.TryGetValue(sessionId, out IClientSession? session))
        {
            _logger.LogDebug("Initiating disconnect for session {SessionId} due to: {Reason}", sessionId, reason);
            await session.RequestShutdownAsync(reason);
        }
    }
}