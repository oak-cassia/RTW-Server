namespace RTWWebServer.Database.Cache;

public interface IRemoteCacheKeyGenerator
{
    string GenerateUserLockKey(int userId);
    string GenerateUserSessionKey(int userId);
}