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
    private readonly ILogger<AsyncAwaitServer> _logger;

    private readonly IClientSessionManager _clientSessionManager;

    public AsyncAwaitServer(
        IServerListener serverListener,
        ILoggerFactory loggerFactory,
        IClientSessionManager clientSessionManager
    )
    {
        _serverListener = serverListener;
        _logger = loggerFactory.CreateLogger<AsyncAwaitServer>();
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
        _logger.LogDebug("Handing off client to ClientSessionManager");

        try
        {
            await _clientSessionManager.HandleNewClientAsync(client, token);
        }
        catch (Exception ex)
        {
            // HandleNewClientAsync 또는 그 내부에서 발생한 예외는 해당 위치에서 로깅 및 처리가 우선되어야 합니다.
            // 이 catch 블록은 정말 예외적인 상황(예: HandleNewClientAsync 자체가 throw)을 위한 것입니다.
            _logger.LogError(ex, "Unhandled exception during client handoff to ClientSessionManager for client: {ClientIdentifier}", client.ToString());
            // client.Close()는 ClientSession의 Disconnect 메서드 또는 ClientSessionManager의 정리 로직에서 담당합니다.
            // 여기서 직접 client.Close()를 호출하면 이중 해제 시도가 발생할 수 있습니다.
        }
    }
}
