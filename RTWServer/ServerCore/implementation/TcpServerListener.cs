using System.Net;
using System.Net.Sockets;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class TcpServerListener : IServerListener
{
    private const SocketOptionLevel SOCKET_OPTION_LEVEL = SocketOptionLevel.Tcp;
    private const SocketOptionName SOCKET_OPTION_NAME = SocketOptionName.NoDelay;

    private readonly TcpListener _listener;

    public TcpServerListener(IPEndPoint endPoint)
    {
        _listener = new TcpListener(endPoint);
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
        TcpClient tcpClient = await _listener.AcceptTcpClientAsync(token);
        tcpClient.Client.SetSocketOption(SOCKET_OPTION_LEVEL, SOCKET_OPTION_NAME, true);
        
        return new TcpClientImpl(tcpClient);
    }
}