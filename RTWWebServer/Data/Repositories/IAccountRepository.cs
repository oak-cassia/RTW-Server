using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public interface IAccountRepository
{
    Task<Account?> FindByEmailAsync(string email);
    Task<Account?> FindByGuidAsync(string guid);
    Task<Account> AddAsync(Account account);
    Task<Account?> GetByIdAsync(int id);
    void Update(Account account);
    void Delete(Account account);
}