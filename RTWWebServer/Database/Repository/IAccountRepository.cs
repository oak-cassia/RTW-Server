using NetworkDefinition.ErrorCode;
using RTWWebServer.Database.Data;

namespace RTWWebServer.Database.Repository;

public interface IAccountRepository
{
    public Task<Account?> FindById(int id);
    public Task<Account?> FindByEmail(string email);
    public Task<WebServerErrorCode> CreateAccount(string username, string email, string password, string salt);
}