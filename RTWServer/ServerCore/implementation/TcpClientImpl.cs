using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class TcpClientImpl : IClient
{
    private readonly TcpClient _client;
    private readonly string _clientId;
    private readonly ILogger _logger;

    public TcpClientImpl(TcpClient client, ILoggerFactory loggerFactory)
    {
        _client = client;
        _logger = loggerFactory.CreateLogger<TcpClientImpl>();
        _clientId = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Stream = client.GetStream();

        _logger.LogDebug("TCP client created for {ClientEndPoint}", _clientId);
    }

    public Stream Stream { get; }
    public bool IsConnected => _client.Connected;

    public async ValueTask<int> ReceiveAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogTrace("Receiving up to {ByteCount} bytes from client {ClientEndPoint}", length, _clientId);
            int bytesRead = await _client.GetStream().ReadAsync(buffer.AsMemory(offset, length), cancellationToken);
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
            _logger.LogInformation("TCP client {ClientEndPoint} closed.", _clientId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while closing connection to client {ClientEndPoint}", _clientId);
        }
    }

    public async Task SendAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogTrace("Sending {ByteCount} bytes to client {ClientEndPoint}", buffer.Length, _clientId);
            await _client.GetStream().WriteAsync(buffer, 0, buffer.Length, cancellationToken);
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
}