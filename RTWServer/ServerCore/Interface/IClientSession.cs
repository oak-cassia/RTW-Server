namespace RTWServer.ServerCore.Interface;

public interface IClientSession
{
    string Id { get; } // Session ID, will also serve as Player ID after authentication
    string? AuthToken { get; } 
    bool IsAuthenticated { get; } 
    
    Task StartSessionAsync(CancellationToken token);
    
    Task SendAsync(IPacket packet);

    // PlayerId in the return tuple now refers to the authenticated Session ID (or a mapping if needed)
    Task<(NetworkDefinition.ErrorCode.RTWErrorCode ErrorCode, int PlayerId)> ValidateAuthTokenAsync(string authToken); 
}