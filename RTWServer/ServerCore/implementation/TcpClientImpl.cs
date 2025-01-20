using System.Net.Sockets;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class TcpClientImpl(TcpClient client) : IClient
{
    public Stream Stream { get; } = client.GetStream();
    public bool IsConnected => client.Connected;

    public async Task SendAsync(byte[] buffer)
    {
        await client.GetStream().WriteAsync(buffer, 0, buffer.Length);
    }

    public async Task<int> ReceiveAsync(byte[] buffer, int offset, int length)
    {
        return await client.GetStream().ReadAsync(buffer, offset, length);
    }

    public void Close()
    {
        client.Close();
    }
}