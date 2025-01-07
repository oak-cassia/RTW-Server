using System.Net.Sockets;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class TcpClientImpl : IClient
{
    private readonly TcpClient _client;
    
    public TcpClientImpl(TcpClient client)
    {
        _client = client;
    }
    
    public string Id { get; } = Guid.NewGuid().ToString();
    
    public bool IsConnected => _client.Connected;
    public async Task SendAsync(byte[] buffer)
    {
        await _client.GetStream().WriteAsync(buffer, 0, buffer.Length);
    }

    public async Task<int> ReadAsync(byte[] buffer, int offset, int length)
    {
        return await _client.GetStream().ReadAsync(buffer, offset, length); 
    }

    public void Close()
    {
        _client.Close();
    }
}