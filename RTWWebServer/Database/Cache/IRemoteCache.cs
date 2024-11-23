using NetworkDefinition.ErrorCode;

namespace RTWWebServer.Database.Cache;

public interface IRemoteCache
{
    Task<(T? value, WebServerErrorCode errorCode)> GetAsync<T>(string key);
    Task<WebServerErrorCode> SetAsync<T>(string key, T value);
    Task<WebServerErrorCode> DeleteAsync(string key);
    Task<WebServerErrorCode> LockAsync(string key, string lockValue);
    Task<WebServerErrorCode> UnlockAsync(string key, string lockValue);
}