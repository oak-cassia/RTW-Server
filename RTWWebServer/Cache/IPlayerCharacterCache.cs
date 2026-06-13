using RTWWebServer.Data.Entities;

namespace RTWWebServer.Cache;

// 플레이어 캐릭터 목록 전용 캐시(cache-aside). 분산 캐시 위에 얇게 올려 짧은 TTL로 stale 창을 제한한다.
public interface IPlayerCharacterCache
{
    Task<List<PlayerCharacter>?> GetAsync(long userId, CancellationToken cancellationToken = default);
    Task SetAsync(long userId, List<PlayerCharacter> characters, CancellationToken cancellationToken = default);
    Task InvalidateAsync(long userId, CancellationToken cancellationToken = default);
}
