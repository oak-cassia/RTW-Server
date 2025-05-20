using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetworkDefinition.ErrorCode;

namespace RTWServer.Auth;

/// <summary>
/// Simple implementation of IUserStateManager that always returns success
/// This is a placeholder implementation that can be replaced with actual authentication logic
/// Thread-safe implementation for use in a multi-threaded environment
/// </summary>
public class UserStateManager : IUserStateManager
{
    private readonly ILogger<UserStateManager> _logger;
    private readonly ConcurrentDictionary<int, UserState> _userStates = new();
    
    // For auth token lookups during initial authentication
    private readonly ConcurrentDictionary<string, int> _authTokenToPlayerId = new();
    
    private int _nextPlayerId = 1; // Simple auto-incrementing ID generator

    public UserStateManager(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<UserStateManager>();
    }

    /// <summary>
    /// Gets the user state for the specified user ID
    /// </summary>
    /// <param name="userId">The user ID to get the state for</param>
    /// <returns>The user state, or a new user state if not found</returns>
    public UserState GetUserStateAsync(string userId)
    {
        _logger.LogDebug("Getting user state for user ID: {UserId}", userId);
        
        // Try to find a UserState that matches this userId
        foreach (var state in _userStates.Values)
        {
            if (state.UserId == userId)
            {
                return state;
            }
        }
        
        // If not found, create a new one with a new player ID
        int playerId = Interlocked.Increment(ref _nextPlayerId);
        var userState = new UserState
        {
            UserId = userId,
            PlayerId = playerId
        };
        
        _userStates.TryAdd(playerId, userState);
        return userState;
    }

    public async Task<(RTWErrorCode ErrorCode, int PlayerId)> ValidateAuthTokenAsync(string authToken)
    {
        // check redis for auth token
        // if found, register player and return player id
        // if not found, return error
        return (RTWErrorCode.Success, 0);
    }
}
