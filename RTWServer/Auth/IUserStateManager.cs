using NetworkDefinition.ErrorCode;

namespace RTWServer.Auth;

public interface IUserStateManager
{
    Task<UserState> GetUserStateAsync(string userId);
    
    /// <summary>
    /// Validates an authentication token received from a client
    /// </summary>
    /// <param name="authToken">The authentication token to validate</param>
    /// <param name="playerId">The player ID if validation succeeds, 0 otherwise</param>
    /// <returns>Error code indicating success or specific failure reason</returns>
    Task<(RTWErrorCode ErrorCode, int PlayerId)> ValidateAuthTokenAsync(string authToken);
}