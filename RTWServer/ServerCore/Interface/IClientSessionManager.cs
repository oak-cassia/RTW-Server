namespace RTWServer.ServerCore.Interface;

public interface IClientSessionManager
{
    // Added: Method to create and add a new client session
    IClientSession CreateClientSession(IClient client, IPacketHandler packetHandler, IPacketSerializer packetSerializer, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory);

    void AddClientSession(IClientSession clientSession); // Kept for flexibility, though CreateClientSession might be preferred
    
    void RemoveClientSession(string id);
    
    IClientSession? GetClientSession(string id);
    
    IEnumerable<IClientSession> GetAllClientSessions();
}