using System.Collections.Concurrent;

namespace RTWWebServer.Database.Cache;

public class RequestScopedCache : IRequestScopedCache
{
    private readonly ConcurrentDictionary<string, object> _cache = new();

    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out var value))
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