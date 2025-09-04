namespace RTWWebServer.Cache;

public interface IRemoteCacheKeyGenerator
{
    string GenerateAccountLockKey(long accountId);
    string GenerateUserSessionKey(long userId);
    string GenerateUserKey(long userId);
    string GeneratePlayerCharactersKey(long userId);
}