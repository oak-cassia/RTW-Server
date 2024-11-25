using RTWWebServer.Authentication;
using RTWWebServer.Database;
using RTWWebServer.Database.Repository;

namespace RTWWebServer.Service;

public class AccountService(
    AccountDatabaseContext databaseContext,
    IAccountRepository accountRepository,
    IGuestRepository guestRepository,
    IPasswordHasher passwordHasher,
    IGuidGenerator guidGenerator,
    ILogger<LoginService> logger
) : IAccountService
{
    public async Task<bool> CreateAccountAsync(string userName, string email, string password)
    {
        await databaseContext.BeginTransactionAsync();

        try
        {
            var salt = passwordHasher.GenerateSaltValue();
            var hashedPassword = passwordHasher.CalcHashedPassword(password, salt);

            // TODO: 기본 데이터 생성, 유저 id 가져와서 Account 테이블에 입력

            var result = await accountRepository.CreateAccountAsync(userName, email, hashedPassword, salt);
            if (result == false)
            {
                await databaseContext.RollbackTransactionAsync();

                return false;
            }

            await databaseContext.CommitTransactionAsync();

            return true;
        }
        catch (Exception)
        {
            await databaseContext.RollbackTransactionAsync();

            throw;
        }
    }

    public async Task<string> CreateGuestAccountAsync()
    {
        await databaseContext.BeginTransactionAsync();

        try
        {
            var guid = guidGenerator.GenerateGuid(); // 고유 식별자

            // TODO: 기본 데이터 생성, 유저 id 가져와서 guest 테이블에 입력

            var result = await guestRepository.CreateGuestAsync(guid.ToByteArray());
            if (result <= 0)
            {
                await databaseContext.RollbackTransactionAsync();

                throw new Exception("Failed to create guest account");
            }

            await databaseContext.CommitTransactionAsync();

            return guid.ToString(); 
        }
        catch (Exception)
        {
            await databaseContext.RollbackTransactionAsync();

            throw;
        }
    }
}