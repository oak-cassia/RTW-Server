namespace RTWWebServer.Cache;

public interface IRemoteCacheKeyGenerator
{
    string GenerateUserLockKey(int userId);
    string GenerateUserSessionKey(int userId);
}