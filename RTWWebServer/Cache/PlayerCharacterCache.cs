using RTWWebServer.Data.Entities;

namespace RTWWebServer.Cache;

public class PlayerCharacterCache(IDistributedCacheAdapter distributedCache, IRemoteCacheKeyGenerator keyGenerator) : IPlayerCharacterCache
{
    // 짧은 TTL: 락 없는 GET이 무효화 직후 stale을 다시 써넣어도 staleness를 이 시간으로 한정한다.
    // 완전 정합(락 내 read-through/버저닝)이 아니라 staleness 창 단축이 목적이다.
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(60);

    public async Task<List<PlayerCharacter>?> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await distributedCache.GetAsync<List<PlayerCharacter>>(keyGenerator.GeneratePlayerCharactersKey(userId), cancellationToken);
    }

    public async Task SetAsync(long userId, List<PlayerCharacter> characters, CancellationToken cancellationToken = default)
    {
        await distributedCache.SetAsync(keyGenerator.GeneratePlayerCharactersKey(userId), characters, CacheExpiration, cancellationToken);
    }

    public async Task InvalidateAsync(long userId, CancellationToken cancellationToken = default)
    {
        await distributedCache.RemoveAsync(keyGenerator.GeneratePlayerCharactersKey(userId), cancellationToken);
    }
}
