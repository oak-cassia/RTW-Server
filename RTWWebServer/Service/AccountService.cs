using RTWWebServer.Authentication;
using RTWWebServer.Database;
using RTWWebServer.Repository;
using Microsoft.EntityFrameworkCore;

namespace RTWWebServer.Service;

public class AccountService(
    AccountDbContext dbContext, 
    IAccountRepository accountRepository,
    IGuestRepository guestRepository,
    IPasswordHasher passwordHasher,
    IGuidGenerator guidGenerator,
    ILogger<AccountService> logger
) : IAccountService
{
    public async Task<bool> CreateAccountAsync(string userName, string email, string password)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            string salt = passwordHasher.GenerateSaltValue();
            string hashedPassword = passwordHasher.CalcHashedPassword(password, salt);

            // TODO: 기본 데이터 생성, 유저 id 가져와서 Account 테이블에 입력

            bool result = await accountRepository.CreateAccountAsync(userName, email, hashedPassword, salt);
            if (result == false)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string> CreateGuestAccountAsync()
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            Guid guid = guidGenerator.GenerateGuid(); // 고유 식별자

            // TODO: 기본 데이터 생성, 유저 id 가져와서 guest 테이블에 입력

            long result = await guestRepository.CreateGuestAsync(guid.ToByteArray());
            if (result <= 0)
            {
                await transaction.RollbackAsync();
                throw new Exception("Failed to create guest account");
            }

            await transaction.CommitAsync();
            return guid.ToString();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}