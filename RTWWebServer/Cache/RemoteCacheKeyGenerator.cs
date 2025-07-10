namespace RTWWebServer.Cache;

public class RemoteCacheKeyGenerator : IRemoteCacheKeyGenerator
{
    public string GenerateUserLockKey(int userId)
    {
        return $"auth_{userId}";
    }

    public string GenerateUserSessionKey(long userId)
    {
        return $"session_{userId}";
    }
}