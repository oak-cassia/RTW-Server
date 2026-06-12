using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

class AsyncAwaitServer
{
    // 대기 중인 연결 요청의 최대 수
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
                IClient client;
                try
                {
                    client = await _serverListener.AcceptClientAsync(token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Server shutdown requested via cancellation token");
                    break;
                }
                catch (SocketException ex)
                {
                    // 개별 연결의 일시적 오류(핸드셰이크 중 리셋 등)로 accept 루프 전체가 죽으면 안 된다
                    _logger.LogWarning(ex, "Socket error while accepting client connection, continuing to accept");
                    continue;
                }

                int currentCount = Interlocked.Increment(ref _acceptCount);
                _logger.LogDebug("Client connection accepted. Total accepted: {AcceptCount}", currentCount);

                _ = HandleClient(client, token);
            }
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
            _logger.LogError(ex, "Unhandled exception during client handoff to ClientSessionManager for client: {ClientIdentifier}", client.ToString());
            // client.Close()는 ClientSession의 Disconnect 메서드 또는 ClientSessionManager의 정리 로직에서 담당합니다.
            // 여기서 직접 client.Close()를 호출하면 이중 해제 시도가 발생할 수 있습니다.
        }
    }
}