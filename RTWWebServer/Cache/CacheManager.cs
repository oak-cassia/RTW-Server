using System.Collections.Concurrent;
using NetworkDefinition.ErrorCode;

namespace RTWWebServer.Cache;

public class CacheManager(
    IRequestScopedLocalCache localCache,
    IRemoteCache remoteCache
) : ICacheManager
{
    private ConcurrentQueue<string> _dirtyKey = new();

    public async Task<T?> GetAsync<T>(string key)
    {
        T? localValue = localCache.Get<T>(key);
        if (localValue != null)
        {
            return localValue;
        }

        (T? remoteValue, WebServerErrorCode errorCode) = await remoteCache.GetAsync<T>(key);
        if (errorCode == WebServerErrorCode.Success)
        {
            localCache.Set(key, remoteValue);
            return remoteValue;
        }

        return default;
    }

    public void Set<T>(string key, T value)
    {
        localCache.Set(key, value);

        if (value == null)
        {
            return;
        }

        _dirtyKey.Enqueue(key);
    }

    public async Task DeleteAsync(string key)
    {
        localCache.Remove(key);
        await remoteCache.DeleteAsync(key);
    }

    public async Task CommitAllChangesAsync()
    {
        while (_dirtyKey.TryDequeue(out string? key))
        {
            object? value = localCache.Get<object>(key);
            if (value == null)
            {
                await remoteCache.DeleteAsync(key);
            }
            else
            {
                await remoteCache.SetAsync(key, value);
            }
        }

        _dirtyKey.Clear();
    }

    public void RollbackAllChanges()
    {
        _dirtyKey.Clear();
    }
}