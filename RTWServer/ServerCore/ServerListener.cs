using System.Net;
using System.Net.Sockets;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore;

public class ServerListener
{
    private const SocketOptionLevel SOCKET_OPTION_LEVEL = SocketOptionLevel.Tcp;
    private const SocketOptionName SOCKET_OPTION_NAME = SocketOptionName.NoDelay;

    private readonly TcpListener _listener;
    private readonly IClientFactory _clientFactory;

    public ServerListener(IPEndPoint endPoint, IClientFactory clientFactory)
    {
        _listener = new TcpListener(endPoint);
        _clientFactory = clientFactory;
    }

    public void Start(int backlog)
    {
        _listener.Start(backlog);
    }

    public void Stop()
    {
        _listener.Stop();
    }

    public async Task<IClient> AcceptClientAsync(CancellationToken token)
    {
        // token 취소 시 TaskCanceledException 발생
        var tcpClient = await _listener.AcceptTcpClientAsync(token);
        tcpClient.Client.SetSocketOption(SOCKET_OPTION_LEVEL, SOCKET_OPTION_NAME, true);

        IClient client = _clientFactory.CreateClient(tcpClient);
        return client;
    }
}