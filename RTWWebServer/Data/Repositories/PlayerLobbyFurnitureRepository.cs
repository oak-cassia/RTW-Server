using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public class PlayerLobbyFurnitureRepository(GameDbContext context) : IPlayerLobbyFurnitureRepository
{
    public async Task<IEnumerable<PlayerLobbyFurniture>> GetByUserIdAsync(long userId)
    {
        return await context.PlayerLobbyFurniture
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Id)
            .ToListAsync();
    }

    public async Task RemoveByUserIdAsync(long userId)
    {
        var existing = await context.PlayerLobbyFurniture
            .Where(f => f.UserId == userId)
            .ToListAsync();

        context.PlayerLobbyFurniture.RemoveRange(existing);
    }

    public async Task AddRangeAsync(IEnumerable<PlayerLobbyFurniture> items)
    {
        await context.PlayerLobbyFurniture.AddRangeAsync(items);
    }
}
