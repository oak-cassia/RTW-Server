using NetworkDefinition.ErrorCode;
using RTWWebServer.Database.Data;

namespace RTWWebServer.Database.Repository;

public interface IAccountRepository
{
    public Task<Account?> FindByIdAsync(int id);
    public Task<Account?> FindByEmailAsync(string email);
    public Task<bool> CreateAccountAsync(string username, string email, string password, string salt);
}