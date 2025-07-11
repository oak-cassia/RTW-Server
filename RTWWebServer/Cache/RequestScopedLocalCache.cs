using System.Collections.Concurrent;

namespace RTWWebServer.Cache;

public class RequestScopedLocalCache : IRequestScopedLocalCache
{
    private readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();

    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out object? value))
        {
            return (T)value;
        }

        return default;
    }

    public void Set<T>(string key, T value)
    {
        if (value != null)
        {
            _cache[key] = value;
        }
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }
}