using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public interface IPlayerLobbyFurnitureRepository
{
    Task<IEnumerable<PlayerLobbyFurniture>> GetByUserIdAsync(long userId);

    // 레이아웃 교체용. SaveChanges/트랜잭션은 서비스가 소유하므로 여기서는 트래킹 변경만 적용한다.
    Task RemoveByUserIdAsync(long userId);
    Task AddRangeAsync(IEnumerable<PlayerLobbyFurniture> items);
}
