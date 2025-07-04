using RTWWebServer.Entities;

namespace RTWWebServer.Repositories;

public interface IAccountRepository
{
    public Task<Account?> FindByIdAsync(int id);
    public Task<Account?> FindByEmailAsync(string email);
    public Task<bool> CreateAccountAsync(string username, string email, string password, string salt);
}