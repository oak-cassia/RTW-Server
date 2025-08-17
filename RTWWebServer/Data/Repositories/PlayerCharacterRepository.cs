using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public class PlayerCharacterRepository(GameDbContext context) : IPlayerCharacterRepository
{
    public async Task<PlayerCharacter?> GetByUserIdAndCharacterIdAsync(long userId, int characterMasterId)
    {
        return await context.PlayerCharacters
            .FirstOrDefaultAsync(pc => pc.UserId == userId && pc.CharacterMasterId == characterMasterId);
    }

    public async Task<IEnumerable<PlayerCharacter>> GetByUserIdAsync(long userId)
    {
        return await context.PlayerCharacters
            .Where(pc => pc.UserId == userId)
            .OrderBy(pc => pc.CharacterMasterId)
            .ToListAsync();
    }

    public async Task<PlayerCharacter> AddAsync(PlayerCharacter playerCharacter)
    {
        var entity = await context.PlayerCharacters.AddAsync(playerCharacter);
        return entity.Entity;
    }

    public async Task UpdateAsync(PlayerCharacter playerCharacter)
    {
        playerCharacter.UpdatedAt = DateTime.UtcNow;
        context.PlayerCharacters.Update(playerCharacter);
        await Task.CompletedTask;
    }
}