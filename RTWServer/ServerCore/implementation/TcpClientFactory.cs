using System.Net.Sockets;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class TcpClientFactory : IClientFactory
{
    public IClient CreateClient(TcpClient client)
    {
        return new TcpClientImpl(client);
    }
}