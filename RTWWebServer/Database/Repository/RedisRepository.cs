using CloudStructures;
using CloudStructures.Structures;
using Microsoft.Extensions.Options;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Configuration;

namespace RTWWebServer.Database.Repository;

public class RedisRepository
{
    private readonly RedisConnection _redisConnection;
    private readonly ILogger<RedisRepository> _logger;

    private readonly TimeSpan _lockExpirationTime = TimeSpan.FromSeconds(3);
    private readonly TimeSpan _stageExpirationTime = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _loginExpirationTime = TimeSpan.FromMinutes(60);
    private readonly TimeSpan _chatExpirationTime = TimeSpan.FromMinutes(120);
    private readonly TimeSpan _authTokenExpirationTime = TimeSpan.FromHours(24);

    public RedisRepository(IOptions<DatabaseConfiguration> configuration, ILogger<RedisRepository> logger)
    {
        var config = new RedisConfig("default", configuration.Value.Redis);
        _redisConnection = new RedisConnection(config);
        _logger = logger;
    }

    public async Task<WebServerErrorCode> SetAsync<T>(string key, T value)
    {
        var redisString = new RedisString<T>(_redisConnection, key, _authTokenExpirationTime);

        if (await redisString.SetAsync(value) == false)
        {
            _logger.LogError($"Failed to set value to Redis with key {key}");
            return WebServerErrorCode.RedisError;
        }

        return WebServerErrorCode.Success;
    }
}