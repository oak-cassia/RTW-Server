using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public interface IAccountRepository
{
    public Task<Account?> FindByIdAsync(int id);
    public Task<Account?> FindByEmailAsync(string email);
    public Task CreateAccountAsync(Account account);
}