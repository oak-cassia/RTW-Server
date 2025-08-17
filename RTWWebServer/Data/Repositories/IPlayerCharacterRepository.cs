using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public interface IPlayerCharacterRepository
{
    Task<PlayerCharacter?> GetByUserIdAndCharacterIdAsync(long userId, int characterMasterId);
    Task<List<PlayerCharacter>> GetByUserIdAsync(long userId);
    Task<PlayerCharacter> AddAsync(PlayerCharacter playerCharacter);
    Task UpdateAsync(PlayerCharacter playerCharacter);
}