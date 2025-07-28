using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public class UserRepository(GameDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(long id)
    {
        return await dbContext.Users.FindAsync(id);
    }

    public async Task<User?> GetByGuidAsync(string guid)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Guid == guid);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
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
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        dbContext.Users.Add(user);
        var test = dbContext.ChangeTracker.Entries();
        await dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var user = await dbContext.Users.FindAsync(id);
        if (user == null)
            return false;

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByGuidAsync(string guid)
    {
        return await dbContext.Users.AnyAsync(u => u.Guid == guid);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await dbContext.Users.AnyAsync(u => u.Email == email);
    }
}