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
        bool success = await remoteCache.SetAsync(sessionKey, userSession, SessionExpiration);

        if (!success)
        {
            throw new GameException("Failed to create user session", WebServerErrorCode.RemoteCacheError);
        }

        logger.LogInformation($"User session created for userId: {userId}, authToken: {authToken}");
        return userSession;
    }

    public async Task<UserSession?> GetSessionAsync(int userId)
    {
        string sessionKey = keyGenerator.GenerateUserSessionKey(userId);
        var session = await remoteCache.GetAsync<UserSession>(sessionKey);

        if (session == null)
        {
            logger.LogDebug($"User session not found or expired for userId: {userId}");
            return null;
        }

        return session;
    }

    public async Task<bool> RemoveSessionAsync(int userId)
    {
        string sessionKey = keyGenerator.GenerateUserSessionKey(userId);
        bool success = await remoteCache.DeleteAsync(sessionKey);

        if (!success)
        {
            // 실패 시에도 false를 반환하기 전에 로깅 가능
            logger.LogError($"Failed to remove user session for userId: {userId}");
            return false;
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