using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class TcpServerListener(IPEndPoint endPoint, ILoggerFactory loggerFactory) : IServerListener
{
    private const SocketOptionLevel SOCKET_OPTION_LEVEL = SocketOptionLevel.Tcp;
    private const SocketOptionName SOCKET_OPTION_NAME = SocketOptionName.NoDelay;

    private readonly TcpListener _listener = new TcpListener(endPoint);
    private readonly ILogger _logger = loggerFactory.CreateLogger<TcpServerListener>();

    public void Start(int backlog)
    {
        try
        {
            _listener.Start(backlog);
            _logger.LogInformation("TCP server listener started on {EndPoint} with backlog {Backlog}",
                endPoint, backlog);
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Failed to start TCP server listener on {EndPoint}", endPoint);
            throw;
        }
    }

    public void Stop()
    {
        try
        {
            _listener.Stop();
            _logger.LogInformation("TCP server listener stopped on {EndPoint}", endPoint);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while stopping TCP server listener on {EndPoint}", endPoint);
        }
    }

    public async Task<IClient> AcceptClientAsync(CancellationToken token)
    {
        try
        {
            // token 취소 시 TaskCanceledException 발생
            _logger.LogDebug("Waiting for client connection on {EndPoint}", endPoint);
            TcpClient tcpClient = await _listener.AcceptTcpClientAsync(token);

            string clientEndPoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
            _logger.LogDebug("Client connected from {ClientEndPoint}", clientEndPoint);

            tcpClient.Client.SetSocketOption(SOCKET_OPTION_LEVEL, SOCKET_OPTION_NAME, true);
            _logger.LogTrace("TCP_NODELAY option set for client {ClientEndPoint}", clientEndPoint);

            return new TcpClientImpl(tcpClient, loggerFactory);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Accept operation cancelled on {EndPoint}", endPoint);
            throw;
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Socket error while accepting client on {EndPoint}", endPoint);
            throw;
        }
    }
}