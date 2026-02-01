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

    public string GenerateUserKey(long userId)
    {
        return $"user:user:{userId}";
    }

    public string GeneratePlayerCharactersKey(long userId)
    {
        return $"player:characters:{userId}";
    }
}