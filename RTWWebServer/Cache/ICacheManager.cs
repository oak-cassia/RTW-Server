namespace RTWWebServer.Cache;

public interface ICacheManager
{
    Task<T?> GetAsync<T>(string key);
    void Set<T>(string key, T value);
    Task DeleteAsync(string key);
    
    Task CommitAllChangesAsync();
    void RollbackAllChanges();
}