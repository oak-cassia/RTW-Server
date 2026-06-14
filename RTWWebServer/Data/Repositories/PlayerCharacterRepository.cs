using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public class PlayerCharacterRepository(GameDbContext context) : IPlayerCharacterRepository
{
    public async Task<IEnumerable<PlayerCharacter>> GetByUserIdAsync(long userId)
    {
        return await context.PlayerCharacters
            .AsNoTracking()
            .Where(pc => pc.UserId == userId)
            .OrderBy(pc => pc.CharacterMasterId)
            .ToListAsync();
    }

    public async Task<PlayerCharacter> AddAsync(PlayerCharacter playerCharacter)
    {
        var entity = await context.PlayerCharacters.AddAsync(playerCharacter);
        return entity.Entity;
    }
}