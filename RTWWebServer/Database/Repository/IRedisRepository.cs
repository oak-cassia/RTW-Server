using NetworkDefinition.ErrorCode;

namespace RTWWebServer.Database.Repository;

public interface IRedisRepository
{
    Task<WebServerErrorCode> SetAsync<T>(string key, T value);
}