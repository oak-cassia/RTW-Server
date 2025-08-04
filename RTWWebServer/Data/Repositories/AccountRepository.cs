using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public class AccountRepository(AccountDbContext dbContext) : IAccountRepository
{
    public async Task<Account?> FindByEmailAsync(string email)
    {
        return await dbContext.Accounts.FirstOrDefaultAsync(a => a.Email == email);
    }

    public async Task<Account?> FindByGuidAsync(string guid)
    {
        return await dbContext.Accounts.FirstOrDefaultAsync(a => a.Guid == guid);
    }

    public async Task<Account> AddAsync(Account account)
    {
        await dbContext.Accounts.AddAsync(account);
        return account;
    }

    public async Task<Account?> GetByIdAsync(int id)
    {
        return await dbContext.Accounts.FindAsync(id);
    }

    public void Update(Account account)
    {
        dbContext.Accounts.Update(account);
    }

    public void Delete(Account account)
    {
        dbContext.Accounts.Remove(account);
    }
}