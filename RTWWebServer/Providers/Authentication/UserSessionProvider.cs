using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.DTOs;
using RTWWebServer.Exceptions;

namespace RTWWebServer.Providers.Authentication;

public class UserSessionProvider(
    IRemoteCache remoteCache,
    IRemoteCacheKeyGenerator keyGenerator,
    IGuidGenerator guidGenerator,
    ILogger<UserSessionProvider> logger
) : IUserSessionProvider
{
    private static readonly TimeSpan SessionExpiration = TimeSpan.FromDays(1);

    public async Task<UserSession> CreateSessionAsync(int userId, string jwtToken)
    {
        string authToken = guidGenerator.GenerateGuid().ToString();
        var userSession = new UserSession(userId, authToken);

        string sessionKey = keyGenerator.GenerateUserSessionKey(userId);
        WebServerErrorCode result = await remoteCache.SetAsync(sessionKey, userSession, SessionExpiration);

        if (result != WebServerErrorCode.Success)
        {
            throw new GameException("Failed to create user session", WebServerErrorCode.RemoteCacheError);
        }

        logger.LogInformation($"User session created for userId: {userId}, authToken: {authToken}");
        return userSession;
    }

    public async Task<UserSession?> GetSessionAsync(int userId)
    {
        string sessionKey = keyGenerator.GenerateUserSessionKey(userId);
        var sessionResult = await remoteCache.GetAsync<UserSession>(sessionKey);

        // Redis에서 키가 없거나 만료된 경우 null 반환 (정상적인 케이스)
        if (sessionResult.errorCode == WebServerErrorCode.RemoteCacheError && sessionResult.value == null)
        {
            logger.LogDebug($"User session not found or expired for userId: {userId}");
            return null;
        }

        if (sessionResult.errorCode != WebServerErrorCode.Success)
        {
            throw new GameException("Failed to retrieve user session", WebServerErrorCode.RemoteCacheError);
        }

        return sessionResult.value;
    }

    public async Task<bool> RemoveSessionAsync(int userId)
    {
        string sessionKey = keyGenerator.GenerateUserSessionKey(userId);
        var deleteResult = await remoteCache.DeleteAsync(sessionKey);

        if (deleteResult != WebServerErrorCode.Success)
        {
            throw new GameException("Failed to remove user session", WebServerErrorCode.RemoteCacheError);
        }

        logger.LogInformation($"Session removed for userId: {userId}");
        return true;
    }

    public async Task<bool> IsValidSessionAsync(int userId, string token)
    {
        var session = await GetSessionAsync(userId);
        if (session == null)
        {
            // Redis TTL로 자동 만료되었거나 세션이 없는 경우
            logger.LogDebug($"Session not found or expired for userId: {userId}");
            return false;
        }

        if (session.Token != token)
        {
            logger.LogWarning($"Token mismatch for userId: {userId}");
            return false;
        }

        return true;
    }
}