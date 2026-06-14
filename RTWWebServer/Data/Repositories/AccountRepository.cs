using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public class AccountRepository(AccountDbContext dbContext) : IAccountRepository
{
    public async Task<Account?> FindByEmailAsync(string email)
    {
        return await dbContext.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Email == email);
    }

    public async Task<Account?> FindByGuidAsync(string guid)
    {
        return await dbContext.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Guid == guid);
    }

    public async Task<Account> AddAsync(Account account)
    {
        await dbContext.Accounts.AddAsync(account);
        return account;
    }
}