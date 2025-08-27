namespace RTWWebServer.Cache;

public class RemoteCacheKeyGenerator : IRemoteCacheKeyGenerator
{
    public string GenerateAccountLockKey(long accountId)
    {
        return $"lock:account:{accountId}";
    }

    public string GenerateUserSessionKey(long userId)
    {
        return $"session_{userId}";
    }
}