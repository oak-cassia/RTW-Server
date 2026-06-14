using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public interface IPlayerCharacterRepository
{
    Task<IEnumerable<PlayerCharacter>> GetByUserIdAsync(long userId);
    Task<PlayerCharacter> AddAsync(PlayerCharacter playerCharacter);
}