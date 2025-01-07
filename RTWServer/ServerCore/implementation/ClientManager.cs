using System.Collections.Concurrent;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

public class ClientManager
{
    private ConcurrentDictionary<string, IClient> _clients = new ConcurrentDictionary<string, IClient>();
    
    public void AddClient(IClient client)
    {
        _clients[client.Id] = client;
    }
    
    public void RemoveClient(IClient client)
    {
        _clients.TryRemove(client.Id, out _);
        
        client.Close();
    }
    
    public IClient? GetClient(string id)
    {
        return _clients.GetValueOrDefault(id);
    }
    
    public IEnumerable<IClient> GetAllClients()
    {
        return _clients.Values;
    }
}