using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace RTWWebServer.Cache;

public class DistributedCacheAdapter(IDistributedCache distributedCache, IConnectionMultiplexer redis) : IDistributedCacheAdapter
{
    private const int MAX_RETRY_COUNT = 10;
    private const int BASE_LOCK_DELAY_MIN_MS = 50;
    private const int BASE_LOCK_DELAY_MAX_MS = 100;
    private const int LOCK_DELAY_MULTIPLIER = 16;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(24);
    private readonly TimeSpan _lockExpiration = TimeSpan.FromSeconds(30);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        string? jsonData = await distributedCache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(jsonData))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(jsonData);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        string jsonData = JsonSerializer.Serialize(value);

        DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
        };

        await distributedCache.SetStringAsync(key, jsonData, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await distributedCache.RemoveAsync(key, cancellationToken);
    }

    public async Task<bool> LockAsync(string lockKey, string lockValue, CancellationToken cancellationToken = default)
    {
        IDatabase database = redis.GetDatabase();
        var retryCount = 0;
        int baseDelay = Random.Shared.Next(BASE_LOCK_DELAY_MIN_MS, BASE_LOCK_DELAY_MAX_MS);

        while (retryCount < MAX_RETRY_COUNT)
        {
            if (await database.StringSetAsync(lockKey, lockValue, _lockExpiration, When.NotExists))
            {
                return true;
            }

            cancellationToken.ThrowIfCancellationRequested();

            int delay = baseDelay + retryCount * LOCK_DELAY_MULTIPLIER;
            await Task.Delay(delay, cancellationToken);
            retryCount++;
        }

        return false;
    }

    public async Task<bool> UnlockAsync(string lockKey, string lockValue)
    {
        IDatabase database = redis.GetDatabase();

        const string script = @"
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                return redis.call('DEL', KEYS[1])
            else
                return 0
            end";

        RedisResult result = await database.ScriptEvaluateAsync(script, [lockKey], [lockValue]);
        return (long)result == 1;
    }
}