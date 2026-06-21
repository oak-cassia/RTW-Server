using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public interface IPlayerCharacterRepository
{
    Task<IEnumerable<PlayerCharacter>> GetByUserIdAsync(long userId);

    // 소유 검증용 핀포인트 조회. (UserId, CharacterMasterId)는 유니크하므로 최대 1건. 미보유면 null.
    Task<PlayerCharacter?> GetByUserIdAndCharacterMasterIdAsync(long userId, int characterMasterId);

    Task<PlayerCharacter> AddAsync(PlayerCharacter playerCharacter);
}