using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public class UserRepository(GameDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(long id)
    {
        return await dbContext.Users.FindAsync(id);
    }

    public async Task<User?> GetByAccountIdAsync(long accountId)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.AccountId == accountId);
    }

    public async Task<User?> GetByNicknameAsync(string nickname)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Nickname == nickname);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await dbContext.Users.ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        await dbContext.Users.AddAsync(user);
        return user;
    }

    public void Update(User user)
    {
        dbContext.Users.Update(user);
    }


    public void Delete(User user)
    {
        dbContext.Users.Remove(user);
    }

    public async Task<User?> GetByMainCharacterIdAsync(long characterId)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.MainCharacterId == characterId);
    }

    public async Task<bool> IsNicknameTakenAsync(string nickname)
    {
        return await dbContext.Users.AnyAsync(u => u.Nickname == nickname);
    }
}