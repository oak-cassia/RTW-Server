using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Packet;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class ClientSessionManager : IClientSessionManager
{
    // 동시 세션 상한 기본값. 무제한 accept로 세션이 끝없이 쌓이는 것을 막는 어드미션 컨트롤.
    public const int DEFAULT_MAX_CONCURRENT_SESSIONS = 1000;

    private readonly ConcurrentDictionary<string, IClientSession> _clientSessions = new();

    // userId → 현재 세션. 인증 성공 시 등록되며 userId당 최대 하나(단일 세션 강제).
    private readonly UserSessionRegistry _userRegistry = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly IPacketHandler _packetHandler;
    private readonly IPacketSerializer _packetSerializer;
    private readonly ISessionValidator _sessionValidator;
    private readonly ILogger<ClientSessionManager> _logger;
    private readonly int _maxConcurrentSessions;

    private int _sessionCount;

    public ClientSessionManager(
        ILoggerFactory loggerFactory,
        IPacketHandler packetHandler,
        IPacketSerializer packetSerializer,
        ISessionValidator sessionValidator,
        int maxConcurrentSessions = DEFAULT_MAX_CONCURRENT_SESSIONS)
    {
        _loggerFactory = loggerFactory;
        _packetHandler = packetHandler;
        _packetSerializer = packetSerializer;
        _sessionValidator = sessionValidator;
        _maxConcurrentSessions = maxConcurrentSessions > 0 ? maxConcurrentSessions : DEFAULT_MAX_CONCURRENT_SESSIONS;
        _logger = _loggerFactory.CreateLogger<ClientSessionManager>();
    }

    private IClientSession CreateClientSession(IClient client, ILoggerFactory loggerFactoryForSession)
    {
        string sessionId = Guid.NewGuid().ToString();
        var session = new ClientSession(client, _packetHandler, _packetSerializer, _sessionValidator, loggerFactoryForSession, sessionId, OnSessionAuthenticatedAsync);
        _clientSessions[session.Id] = session;
        return session;
    }

    public async Task HandleNewClientAsync(IClient client, CancellationToken token)
    {
        // 어드미션 컨트롤: 동시 세션 상한을 넘으면 세션을 만들지 않고 즉시 닫는다.
        // 미인증 상태라 타입 패킷으로 통보할 수단이 없으므로 연결만 정리한다.
        int currentCount = Interlocked.Increment(ref _sessionCount);
        if (currentCount > _maxConcurrentSessions)
        {
            Interlocked.Decrement(ref _sessionCount);
            _logger.LogWarning("Concurrent session limit {Max} reached, rejecting new connection", _maxConcurrentSessions);
            client.Dispose();
            return;
        }

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

            Interlocked.Decrement(ref _sessionCount);
        }
    }

    // 세션이 인증에 성공하면 호출된다(ClientSession이 콜백으로 주입받음).
    // userId 인덱스에 등록하고, 같은 userId의 기존 세션이 밀려나면 last-wins로 강제 종료한다.
    private async Task OnSessionAuthenticatedAsync(IClientSession session)
    {
        if (session.UserId == 0)
        {
            // 방어적: 인증 성공이면 userId는 0이 아니다.
            return;
        }

        IClientSession? displaced = _userRegistry.Register(session);
        if (displaced != null)
        {
            _logger.LogInformation(
                "Duplicate login for userId {UserId}: kicking previous session {OldSessionId}, replaced by {NewSessionId}",
                session.UserId, displaced.Id, session.Id);
            await displaced.RequestShutdownAsync("Logged in elsewhere");
        }
    }

    public async Task RemoveClientSessionAsync(string id)
    {
        if (_clientSessions.TryRemove(id, out var session))
        {
            // 유저 인덱스에서도 제거. 레지스트리가 pair 단위로 제거하므로 last-wins 교체로
            // 들어온 새 세션은 영향받지 않는다. 미인증(UserId==0) 세션은 인덱스에 없어 무해하다.
            if (session.UserId != 0)
            {
                _userRegistry.Unregister(session);
            }

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

    public IClientSession? GetSessionByUserId(long userId)
    {
        return _userRegistry.Get(userId);
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