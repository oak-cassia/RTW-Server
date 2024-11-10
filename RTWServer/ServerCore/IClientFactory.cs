using System.Net.Sockets;

namespace RTWServer.ServerCore;

public interface IClientFactory
{
    IClient CreateClient(TcpClient client);
}