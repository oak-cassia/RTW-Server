using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
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
    private const int MAX_EMAIL_LENGTH = 64; // AccountDbContext의 Email HasMaxLength와 일치해야 함
    private const int MIN_PASSWORD_LENGTH = 8;

    public async Task CreateAccountAsync(string email, string password)
    {
        ValidateCredentials(email, password);

        string salt = passwordHasher.GenerateSaltValue();
        string hashedPassword = passwordHasher.CalcHashedPassword(password, salt);

        Account account = new Account(email, hashedPassword, salt, UserRole.Normal, DateTime.UtcNow, DateTime.UtcNow);

        try
        {
            await SaveAccountAndValidateAsync(account);
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            // Email unique 인덱스 위반 - 이미 가입된 이메일
            throw new GameException($"Email already in use", WebServerErrorCode.DuplicateEmail);
        }
    }

    private static void ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new GameException("Email and password are required", WebServerErrorCode.InvalidRequestHttpBody);
        }

        if (email.Length > MAX_EMAIL_LENGTH || !new EmailAddressAttribute().IsValid(email))
        {
            throw new GameException("Invalid email format", WebServerErrorCode.InvalidRequestHttpBody);
        }

        if (password.Length < MIN_PASSWORD_LENGTH)
        {
            throw new GameException($"Password must be at least {MIN_PASSWORD_LENGTH} characters", WebServerErrorCode.InvalidRequestHttpBody);
        }
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