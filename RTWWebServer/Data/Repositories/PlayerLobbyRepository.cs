using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public class PlayerLobbyRepository(GameDbContext context) : IPlayerLobbyRepository
{
    public async Task<PlayerLobby?> GetByUserIdAsync(long userId)
    {
        return await context.PlayerLobbies
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.UserId == userId);
    }

    public async Task AddAsync(PlayerLobby lobby)
    {
        await context.PlayerLobbies.AddAsync(lobby);
    }

    public void Update(PlayerLobby lobby)
    {
        context.PlayerLobbies.Update(lobby);
    }
}
