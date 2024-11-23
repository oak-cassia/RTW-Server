using CloudStructures;
using CloudStructures.Structures;
using Microsoft.Extensions.Options;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Configuration;

namespace RTWWebServer.Database.Cache;

public class RedisRemoteCache : IRemoteCache
{
    private const int MAX_RETRY_COUNT = 10;
    private const int MIN_BASE_LOCK_DELAY = 50;
    private const int MAX_BASE_LOCK_DELAY = 101;
    private const int MULTIPLIER_LOCK_DELAY = 4;

    private readonly RedisConnection _redisConnection;
    private readonly ILogger<RedisRemoteCache> _logger;

    private readonly TimeSpan _lockExpirationTime = TimeSpan.FromSeconds(3);
    private readonly TimeSpan _authTokenExpirationTime = TimeSpan.FromHours(24);

    public RedisRemoteCache(IOptions<DatabaseConfiguration> configuration, ILogger<RedisRemoteCache> logger)
    {
        var config = new RedisConfig("default", configuration.Value.Redis);
        _redisConnection = new RedisConnection(config);
        _logger = logger;
    }

    public async Task<(T? value, WebServerErrorCode errorCode)> GetAsync<T>(string key)
    {
        var redisString = new RedisString<T>(_redisConnection, key, null);

        var value = await redisString.GetAsync();
        if (value.HasValue == false)
        {
            _logger.LogError($"Failed to get value from Redis with key {key}");
            return (default, WebServerErrorCode.RemoteCacheError);
        }

        return (value.Value, WebServerErrorCode.Success);
    }


    public async Task<WebServerErrorCode> SetAsync<T>(string key, T value)
    {
        var redisString = new RedisString<T>(_redisConnection, key, _authTokenExpirationTime);

        if (await redisString.SetAsync(value) == false)
        {
            _logger.LogError($"Failed to set value to Redis with key {key}");
            return WebServerErrorCode.RemoteCacheError;
        }

        return WebServerErrorCode.Success;
    }

    public async Task<WebServerErrorCode> DeleteAsync(string key)
    {
        var redisString = new RedisString<string>(_redisConnection, key, null);

        if (await redisString.DeleteAsync() == false)
        {
            _logger.LogError($"Failed to remove value from Redis with key {key}");
            return WebServerErrorCode.RemoteCacheError;
        }

        return WebServerErrorCode.Success;
    }

    public async Task<WebServerErrorCode> LockAsync(string key, string lockValue)
    {
        var redisLock = new RedisLock<string>(_redisConnection, key);

        var retryCount = 0;
        var baseDelay = Random.Shared.Next(MIN_BASE_LOCK_DELAY, MAX_BASE_LOCK_DELAY);

        // 총 대기 시간 범위: 0ms ~ 1720ms
        while (retryCount < MAX_RETRY_COUNT)
        {
            var isLocked = await redisLock.TakeAsync(lockValue, _lockExpirationTime);
            if (isLocked)
            {
                return WebServerErrorCode.Success;
            }

            // base(50~100) + (재시도 횟수 * 16) ms
            var delay = baseDelay + (retryCount << MULTIPLIER_LOCK_DELAY);
            await Task.Delay(delay);

            retryCount++;
        }

        _logger.LogError($"Failed to lock key {key}, retry count: {retryCount}");
        return WebServerErrorCode.RemoteCacheLockFailed;
    }

    public async Task<WebServerErrorCode> UnlockAsync(string key, string lockValue)
    {
        var redisLock = new RedisLock<string>(_redisConnection, key);

        if (await redisLock.ReleaseAsync(lockValue) == false)
        {
            _logger.LogError($"Failed to unlock key {key}, lock value: {lockValue}");
            return WebServerErrorCode.RemoteCacheError;
        }

        return WebServerErrorCode.Success;
    }
}