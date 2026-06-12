namespace RTWWebServer.Cache;

public class RemoteCacheKeyGenerator : IRemoteCacheKeyGenerator
{
    public string GenerateAccountLockKey(long accountId)
    {
        return $"lock:account:{accountId}";
    }

    public string GenerateUserLockKey(long userId)
    {
        return $"lock:user:{userId}";
    }

    public string GenerateUserSessionKey(long userId)
    {
        return $"session_{userId}";
    }

    public string GeneratePlayerCharactersKey(long userId)
    {
        return $"player:characters:{userId}";
    }
}