using RTWWebServer.Cache;
using RTWWebServer.DTOs;

namespace RTWWebServer.Providers.Authentication;

public class UserSessionProvider(
    IDistributedCacheAdapter distributedCache,
    IRemoteCacheKeyGenerator keyGenerator,
    IGuidGenerator guidGenerator,
    ILogger<UserSessionProvider> logger
) : IUserSessionProvider
{
    private static readonly TimeSpan SessionExpiration = TimeSpan.FromDays(1);

    public async Task<UserSession> CreateSessionAsync(long userId, string jwtToken)
    {
        var authToken = guidGenerator.GenerateGuid().ToString();
        UserSession userSession = new UserSession(userId, authToken);

        string sessionKey = keyGenerator.GenerateUserSessionKey(userId);
        await distributedCache.SetAsync(sessionKey, userSession, SessionExpiration);

        logger.LogInformation($"User session created for userId: {userId}, authToken: {authToken}");
        return userSession;
    }

    public async Task<UserSession?> GetSessionAsync(long userId)
    {
        string sessionKey = keyGenerator.GenerateUserSessionKey(userId);
        UserSession? session = await distributedCache.GetAsync<UserSession>(sessionKey);

        if (session == null)
        {
            logger.LogDebug($"User session not found or expired for userId: {userId}");
            return null;
        }

        return session;
    }

    public async Task<bool> RemoveSessionAsync(long userId)
    {
        string sessionKey = keyGenerator.GenerateUserSessionKey(userId);
        await distributedCache.RemoveAsync(sessionKey);

        logger.LogInformation($"Session removed for userId: {userId}");
        return true;
    }

    public async Task<bool> IsValidSessionAsync(long userId, string token)
    {
        UserSession? session = await GetSessionAsync(userId);
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