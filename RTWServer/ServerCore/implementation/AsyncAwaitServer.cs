using Microsoft.Extensions.Logging;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

class AsyncAwaitServer
{
    private const int BACKLOG = 100;

    // 서버 상태를 기록하는 필드
    private int _acceptCount; // 수락된 연결 수

    private readonly ServerListener _serverListener;
    private readonly IPacketHandler _packetHandler;
    private readonly ILogger _logger;

    private readonly IPacketFactory _packetFactory;
    private readonly ClientManager _clientManager;

    public AsyncAwaitServer(
        ServerListener serverListener,
        IPacketHandler packetHandler,
        ILoggerFactory loggerFactory,
        IPacketFactory packetFactory,
        ClientManager clientManager
    )
    {
        _serverListener = serverListener;
        _packetHandler = packetHandler;
        _logger = loggerFactory.CreateLogger<AsyncAwaitServer>();
        _packetFactory = packetFactory;
        _clientManager = clientManager;
    }

    public async Task Start(CancellationToken token)
    {
        _serverListener.Start(BACKLOG);
        _logger.LogInformation("Server started...");

        try
        {
            while (!token.IsCancellationRequested)
            {
                IClient client = await _serverListener.AcceptClientAsync(token);

                Interlocked.Increment(ref _acceptCount);

                _clientManager.AddClient(client);

                _ = HandleClient(client, token);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Accepting client failed.");
        }
        finally
        {
            _serverListener.Stop();
            _logger.LogInformation("Server stopped.");
        }
    }

    private async Task HandleClient(IClient client, CancellationToken token)
    {
        try
        {
            var session = new ClientSession(client, _packetHandler, _packetFactory);
            await session.StartSessionAsync(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling the client.");
        }
        finally
        {
            _clientManager.RemoveClient(client);
        }
    }
}