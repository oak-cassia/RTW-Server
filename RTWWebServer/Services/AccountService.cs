using NetworkDefinition.ErrorCode;
using RTWWebServer.Data;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.Enums;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.Authentication;

namespace RTWWebServer.Services;

public class AccountService(
    AccountDbContext dbContext,
    IAccountRepository accountRepository,
    IPasswordHasher passwordHasher,
    IGuidGenerator guidGenerator
) : IAccountService
{
    public async Task CreateAccountAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new GameException("Email and password are required", WebServerErrorCode.InvalidRequestHttpBody);
        }

        string salt = passwordHasher.GenerateSaltValue();
        string hashedPassword = passwordHasher.CalcHashedPassword(password, salt);

        Account account = new Account(email, hashedPassword, salt, UserRole.Normal, DateTime.UtcNow, DateTime.UtcNow);

        await SaveAccountAndValidateAsync(account);
    }

    public async Task<string> CreateGuestAccountAsync()
    {
        // device id 검증 생략
        Guid guid = guidGenerator.GenerateGuid();

        Account guestAccount = new Account(guid.ToString(), UserRole.Guest, DateTime.UtcNow, DateTime.UtcNow);

        await SaveAccountAndValidateAsync(guestAccount);

        return guid.ToString();
    }

    private async Task SaveAccountAndValidateAsync(Account account)
    {
        await accountRepository.AddAsync(account);

        await dbContext.SaveChangesAsync();

        if (account.Id <= 0)
        {
            throw new GameException("Failed to create account - ID not generated", WebServerErrorCode.DatabaseError);
        }
    }
}