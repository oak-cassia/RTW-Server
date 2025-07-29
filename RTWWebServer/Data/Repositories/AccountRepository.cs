using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

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

    public Task CreateAccountAsync(Account account)
    {
        dbContext.Accounts.Add(account);
        return Task.CompletedTask;
    }
}