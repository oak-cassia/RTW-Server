namespace RTWWebServer.Database.Cache;

public interface IRequestScopedLocalCache
{
    T? Get<T>(string key);
    void Set<T>(string key, T value);
    void Remove(string key);
}