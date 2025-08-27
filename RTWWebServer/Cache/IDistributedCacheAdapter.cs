namespace RTWWebServer.Cache;

public interface IDistributedCacheAdapter
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> LockAsync(string lockKey, string lockValue, CancellationToken cancellationToken = default);
    Task<bool> UnlockAsync(string lockKey, string lockValue);
}