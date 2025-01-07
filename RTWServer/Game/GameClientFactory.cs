using System.Net.Sockets;
using RTWServer.ServerCore;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game;

public class GameClientFactory : IClientFactory
{
    public IClient CreateClient(TcpClient client)
    {
        return new GameClient(client);
    }
}