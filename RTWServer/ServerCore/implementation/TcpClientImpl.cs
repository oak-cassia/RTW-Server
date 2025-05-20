using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class TcpClientImpl : IClient
{
    private readonly TcpClient _client;
    private readonly ILogger _logger;
    private readonly string _clientId;

    public Stream Stream { get; }
    public bool IsConnected => _client.Connected;

    public TcpClientImpl(TcpClient client, ILoggerFactory loggerFactory)
    {
        _client = client;
        _logger = loggerFactory.CreateLogger<TcpClientImpl>();
        _clientId = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Stream = client.GetStream();

        _logger.LogDebug("TCP client created for {ClientEndPoint}", _clientId);
    }

    public async Task SendAsync(byte[] buffer)
    {
        try
        {
            _logger.LogTrace("Sending {ByteCount} bytes to client {ClientEndPoint}", buffer.Length, _clientId);
            await _client.GetStream().WriteAsync(buffer, 0, buffer.Length);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Network error while sending data to client {ClientEndPoint}", _clientId);
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning(ex, "Connection already closed for client {ClientEndPoint}", _clientId);
            throw;
        }
    }

    public async Task<int> ReceiveAsync(byte[] buffer, int offset, int length)
    {
        try
        {
            _logger.LogTrace("Receiving up to {ByteCount} bytes from client {ClientEndPoint}", length, _clientId);
            int bytesRead = await _client.GetStream().ReadAsync(buffer, offset, length);
            _logger.LogTrace("Received {BytesRead} bytes from client {ClientEndPoint}", bytesRead, _clientId);
            return bytesRead;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Network error while receiving data from client {ClientEndPoint}", _clientId);
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning(ex, "Connection already closed for client {ClientEndPoint}", _clientId);
            throw;
        }
    }

    public void Close()
    {
        try
        {
            _logger.LogDebug("Closing connection to client {ClientEndPoint}", _clientId);
            _client.Close();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while closing connection to client {ClientEndPoint}", _clientId);
        }
    }
}
