using Microsoft.EntityFrameworkCore;
using RTWWebServer.Database;
using RTWWebServer.Entity;

namespace RTWWebServer.Repository;

public class AccountRepository(AccountDbContext dbContext) : IAccountRepository
{
    public async Task<Account?> FindByIdAsync(int id)
    {
        return await dbContext.Accounts.FindAsync((long)id);
    }

    public async Task<Account?> FindByEmailAsync(string email)
    {
        return await dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Email == email);
    }

    public async Task<bool> CreateAccountAsync(string username, string email, string password, string salt)
    {
        var account = new Account(username, email, password, salt);

        dbContext.Accounts.Add(account);
        int rowsAffected = await dbContext.SaveChangesAsync();
        return rowsAffected > 0;
    }
}