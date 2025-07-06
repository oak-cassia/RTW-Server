using NetworkDefinition.ErrorCode;
using RTWWebServer.DTOs.Request;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.Authentication;

namespace RTWWebServer.Services;

public class GameEntryService(
    IJwtTokenProvider jwtTokenProvider,
    IUserSessionProvider userSessionProvider,
    ILogger<GameEntryService> logger
) : IGameEntryService
{
    public async Task<GameEntryResponse> EnterGameAsync(GameEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JwtToken))
        {
            logger.LogWarning("Empty JWT token provided");
            throw new GameException("JWT token is required", WebServerErrorCode.InvalidAuthToken);
        }

        if (!jwtTokenProvider.ValidateJwt(request.JwtToken))
        {
            logger.LogWarning($"Invalid JWT token: {request.JwtToken}");
            throw new GameException("Invalid JWT token", WebServerErrorCode.InvalidAuthToken);
        }

        long? userIdLong = jwtTokenProvider.GetUserIdFromJwt(request.JwtToken);
        if (!userIdLong.HasValue)
        {
            logger.LogError("Failed to extract user ID from JWT token");
            throw new GameException("Failed to extract user ID from JWT token", WebServerErrorCode.InvalidAuthToken);
        }

        int userId = (int)userIdLong.Value;

        var userSession = await userSessionProvider.CreateSessionAsync(userId, request.JwtToken);

        logger.LogInformation($"User {userId} successfully entered the game with auth token: {userSession.Token}");

        return new GameEntryResponse(
            WebServerErrorCode.Success,
            userSession.Token,
            userId
        );
    }
}