using NetworkDefinition.ErrorCode;

namespace RTWWebServer.Cache;

public interface IRemoteCache
{
    Task<T?> GetAsync<T>(string key);
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task<bool> DeleteAsync(string key);
    Task<bool> LockAsync(int userId, string lockValue);
    Task<bool> UnlockAsync(int userId, string lockValue);
}