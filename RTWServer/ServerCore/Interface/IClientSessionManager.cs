namespace RTWServer.ServerCore.Interface;

public interface IClientSessionManager
{
    // Method to handle a new client connection, including session creation and starting the session.
    Task HandleNewClientAsync(IClient client, CancellationToken token);
    
    void RemoveClientSession(string id);
    
    IClientSession? GetClientSession(string id);
    
    IEnumerable<IClientSession> GetAllClientSessions();

    /// <summary>
    /// Initiates a graceful disconnect for the specified client session.
    /// </summary>
    /// <param name="sessionId">The ID of the session to disconnect.</param>
    /// <param name="reason">The reason for the disconnection.</param>
    /// <returns>A task that represents the asynchronous disconnect operation.</returns>
    Task InitiateClientDisconnectAsync(string sessionId, string reason);
}