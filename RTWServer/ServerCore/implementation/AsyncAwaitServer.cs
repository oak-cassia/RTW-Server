using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

class AsyncAwaitServer
{
    // Maximum number of pending connection requests
    private const int MAX_PENDING_CONNECTIONS = 100;

    // 서버 상태를 기록하는 필드
    private int _acceptCount; // 수락된 연결 수

    private readonly IServerListener _serverListener;
    private readonly IPacketHandler _packetHandler;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    private readonly IPacketSerializer _packetSerializer;
    private readonly IClientSessionManager _clientSessionManager;

    public AsyncAwaitServer(
        IServerListener serverListener,
        IPacketHandler packetHandler,
        ILoggerFactory loggerFactory,
        IPacketSerializer packetSerializer,
        IClientSessionManager clientSessionManager
    )
    {
        _serverListener = serverListener;
        _packetHandler = packetHandler;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AsyncAwaitServer>();
        _packetSerializer = packetSerializer;
        _clientSessionManager = clientSessionManager;
    }

    public async Task Start(CancellationToken token)
    {
        _serverListener.Start(MAX_PENDING_CONNECTIONS);
        _logger.LogInformation("Server started with max {MaxConnections} pending connections", MAX_PENDING_CONNECTIONS);

        try
        {
            while (!token.IsCancellationRequested)
            {
                IClient client = await _serverListener.AcceptClientAsync(token);

                int currentCount = Interlocked.Increment(ref _acceptCount);
                _logger.LogDebug("Client connection accepted. Total accepted: {AcceptCount}", currentCount);

                _ = HandleClient(client, token);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Server shutdown requested via cancellation token");
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Socket error while accepting client connection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while accepting client connection");
        }
        finally
        {
            _serverListener.Stop();
            _logger.LogInformation("Server stopped. Total connections accepted: {AcceptCount}", _acceptCount);
        }
    }

    private async Task HandleClient(IClient client, CancellationToken token)
    {
        string sessionId = Guid.NewGuid().ToString();
        _logger.LogDebug("Creating new session {SessionId} for client", sessionId);

        try
        {
            IClientSession session = new ClientSession(
                client, 
                _packetHandler, 
                _packetSerializer, 
                _clientSessionManager,
                _loggerFactory,
                sessionId);

            _clientSessionManager.AddClientSession(session);
            _logger.LogDebug("Session {SessionId} added to session manager", sessionId);

            await session.StartSessionAsync(token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Session {SessionId} cancelled due to server shutdown", sessionId);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Network error for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while handling client session {SessionId}", sessionId);
        }
    }
}
