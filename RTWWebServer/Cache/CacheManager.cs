using System.Collections.Concurrent;

namespace RTWWebServer.Cache;

public class CacheManager(IRequestScopedLocalCache localCache, IDistributedCacheAdapter distributedCache) : ICacheManager
{
    private readonly ConcurrentQueue<string> _dirtyKeys = new ConcurrentQueue<string>();

    public async Task<T?> GetAsync<T>(string key)
    {
        T? localValue = localCache.Get<T>(key);
        if (localValue != null)
        {
            return localValue;
        }

        T? remoteValue = await distributedCache.GetAsync<T>(key);
        if (remoteValue != null)
        {
            localCache.Set(key, remoteValue);
            return remoteValue;
        }

        return default;
    }

    public void Set<T>(string key, T value)
    {
        localCache.Set(key, value);

        if (value != null)
        {
            _dirtyKeys.Enqueue(key);
        }
    }

    public async Task DeleteAsync(string key)
    {
        localCache.Remove(key);
        await distributedCache.RemoveAsync(key);
    }

    public async Task CommitAllChangesAsync()
    {
        while (_dirtyKeys.TryDequeue(out string? key))
        {
            var value = localCache.Get<object>(key);
            if (value != null)
            {
                await distributedCache.SetAsync(key, value);
            }
        }
    }

    public void RollbackAllChanges()
    {
        // 더티 키들을 클리어하고 로컬 캐시의 변경사항을 무시
        while (_dirtyKeys.TryDequeue(out _))
        {
        }

        // 필요시 로컬 캐시도 클리어할 수 있음
        // _localCache.Clear();
    }
}