using System.Security.Cryptography;
using System.Text;
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

    public async Task<UserSession> CreateSessionAsync(long userId, string nickname)
    {
        var authToken = guidGenerator.GenerateGuid().ToString();
        UserSession userSession = new UserSession(userId, authToken, nickname);

        string sessionKey = keyGenerator.GenerateUserSessionKey(userId);
        await distributedCache.SetAsync(sessionKey, userSession, SessionExpiration);

        // authToken은 자격 증명이므로 로그에 남기지 않는다
        logger.LogInformation("User session created for userId: {UserId}", userId);
        return userSession;
    }

    public async Task<UserSession?> GetSessionAsync(long userId)
    {
        string sessionKey = keyGenerator.GenerateUserSessionKey(userId);
        UserSession? session = await distributedCache.GetAsync<UserSession>(sessionKey);

        if (session == null)
        {
            logger.LogDebug("User session not found or expired for userId: {UserId}", userId);
            return null;
        }

        return session;
    }

    public async Task<bool> RemoveSessionAsync(long userId)
    {
        string sessionKey = keyGenerator.GenerateUserSessionKey(userId);
        await distributedCache.RemoveAsync(sessionKey);

        logger.LogInformation("Session removed for userId: {UserId}", userId);
        return true;
    }

    public async Task<bool> IsValidSessionAsync(long userId, string token)
    {
        UserSession? session = await GetSessionAsync(userId);
        if (session == null)
        {
            // Redis TTL로 자동 만료되었거나 세션이 없는 경우
            logger.LogDebug("Session not found or expired for userId: {UserId}", userId);
            return false;
        }

        // 타이밍 공격으로 토큰을 한 글자씩 추측할 수 없도록 상수 시간 비교를 사용한다
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(session.Token),
                Encoding.UTF8.GetBytes(token)))
        {
            logger.LogWarning("Token mismatch for userId: {UserId}", userId);
            return false;
        }

        return true;
    }
}