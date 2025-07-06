using CloudStructures;
using CloudStructures.Structures;
using Microsoft.Extensions.Options;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Configuration;

namespace RTWWebServer.Cache;

public class RedisRemoteCache : IRemoteCache
{
    private const int MAX_RETRY_COUNT = 10;
    private const int MIN_BASE_LOCK_DELAY = 50;
    private const int MAX_BASE_LOCK_DELAY = 101;
    private const int MULTIPLIER_LOCK_DELAY = 4;
    private readonly TimeSpan _authTokenExpirationTime = TimeSpan.FromHours(24);
    private readonly TimeSpan _lockExpirationTime = TimeSpan.FromSeconds(3);

    private readonly IRemoteCacheKeyGenerator _keyGenerator;
    private readonly ILogger<RedisRemoteCache> _logger;

    private readonly RedisConnection _redisConnection;

    public RedisRemoteCache(IOptions<DatabaseConfiguration> configuration, IRemoteCacheKeyGenerator keyGenerator, ILogger<RedisRemoteCache> logger)
    {
        RedisConfig config = new RedisConfig("default", configuration.Value.Redis);
        _redisConnection = new RedisConnection(config);
        _keyGenerator = keyGenerator;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        RedisString<T> redisString = new RedisString<T>(_redisConnection, key, null);

        RedisResult<T> value = await redisString.GetAsync();
        if (value.HasValue == false)
        {
            return default;
        }

        return value.Value;
    }


    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        TimeSpan actualExpiration = expiration ?? _authTokenExpirationTime;
        RedisString<T> redisString = new RedisString<T>(_redisConnection, key, actualExpiration);

        var success = await redisString.SetAsync(value);
        if (success == false)
        {
            _logger.LogError($"Failed to set value to Redis with key {key} with expiration {actualExpiration}");
        }

        return success;
    }

    public async Task<bool> DeleteAsync(string key)
    {
        RedisString<string> redisString = new RedisString<string>(_redisConnection, key, null);

        var success = await redisString.DeleteAsync();
        if (success == false)
        {
            _logger.LogError($"Failed to remove value from Redis with key {key}");
        }

        return success;
    }

    public async Task<bool> LockAsync(int userId, string lockValue)
    {
        string key = _keyGenerator.GenerateUserLockKey(userId);
        RedisLock<string> redisLock = new RedisLock<string>(_redisConnection, key);

        var retryCount = 0;
        int baseDelay = Random.Shared.Next(MIN_BASE_LOCK_DELAY, MAX_BASE_LOCK_DELAY);

        // 총 대기 시간 범위: 0ms ~ 1720ms
        while (retryCount < MAX_RETRY_COUNT)
        {
            bool isLocked = await redisLock.TakeAsync(lockValue, _lockExpirationTime);
            if (isLocked)
            {
                return true;
            }

            // base(50~100) + (재시도 횟수 * 16) ms
            int delay = baseDelay + (retryCount << MULTIPLIER_LOCK_DELAY);
            await Task.Delay(delay);

            retryCount++;
        }

        _logger.LogError($"Failed to lock key {userId}, retry count: {retryCount}");
        return false;
    }

    public async Task<bool> UnlockAsync(int userId, string lockValue)
    {
        string key = _keyGenerator.GenerateUserLockKey(userId);
        RedisLock<string> redisLock = new RedisLock<string>(_redisConnection, key);

        var success = await redisLock.ReleaseAsync(lockValue);
        if (success == false)
        {
            _logger.LogError($"Failed to unlock key {userId}, lock value: {lockValue}");
        }

        return success;
    }
}