using NetworkDefinition.ErrorCode;
using RTWWebServer.DTOs;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.Authentication;

namespace RTWWebServer.Services;

public class GameEntryService(
    IJwtTokenProvider jwtTokenProvider,
    IUserSessionProvider userSessionProvider,
    ILogger<GameEntryService> logger
) : IGameEntryService
{
    public async Task<UserSession> EnterGameAsync(string jwtToken)
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
        {
            throw new GameException("JWT token is required", WebServerErrorCode.InvalidAuthToken);
        }

        if (!jwtTokenProvider.ValidateJwt(jwtToken))
        {
            throw new GameException("Invalid JWT token", WebServerErrorCode.InvalidAuthToken);
        }

        long? userIdLong = jwtTokenProvider.GetUserIdFromJwt(jwtToken);
        if (!userIdLong.HasValue)
        {
            throw new GameException("Failed to extract user ID from JWT token", WebServerErrorCode.InvalidAuthToken);
        }

        int userId = (int)userIdLong.Value;

        var userSession = await userSessionProvider.CreateSessionAsync(userId, jwtToken);

        logger.LogInformation($"User {userId} successfully entered the game with auth token: {userSession.Token}");

        return userSession;
    }
}