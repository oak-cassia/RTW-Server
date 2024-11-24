namespace RTWWebServer.Database.Cache;

public class RemoteCacheKeyGenerator : IRemoteCacheKeyGenerator
{
    public string GenerateUserLockKey(int userId)
    {
        return $"auth_{userId}";
    }

    public string GenerateUserSessionKey(int userId)
    {
        return $"session_{userId}";
    }
}