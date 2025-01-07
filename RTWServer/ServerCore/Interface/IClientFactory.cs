using System.Net.Sockets;

namespace RTWServer.ServerCore.Interface;

public interface IClientFactory
{
    IClient CreateClient(TcpClient client);
}