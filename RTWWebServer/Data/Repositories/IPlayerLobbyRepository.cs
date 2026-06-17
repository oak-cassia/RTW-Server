using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public interface IPlayerLobbyRepository
{
    // 행이 없으면 null(= 기본 1등급). SaveChanges는 서비스가 소유한다.
    Task<PlayerLobby?> GetByUserIdAsync(long userId);
    Task AddAsync(PlayerLobby lobby);
    void Update(PlayerLobby lobby);
}
