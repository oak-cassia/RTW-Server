using RTWWebServer.Authentication;
using RTWWebServer.Database;
using RTWWebServer.Database.Repository;

namespace RTWWebServer.Service;

public class AccountService(
    IDatabaseContextProvider databaseContextProvider,
    IAccountRepository accountRepository,
    IGuestRepository guestRepository,
    IPasswordHasher passwordHasher,
    IGuidGenerator guidGenerator,
    ILogger<LoginService> logger
) : IAccountService
{
    private readonly IDatabaseContext _databaseContext = databaseContextProvider.GetDatabaseContext("Account");

    public async Task<bool> CreateAccountAsync(string userName, string email, string password)
    {
        await _databaseContext.BeginTransactionAsync();

        try
        {
            string salt = passwordHasher.GenerateSaltValue();
            string hashedPassword = passwordHasher.CalcHashedPassword(password, salt);

            // TODO: 기본 데이터 생성, 유저 id 가져와서 Account 테이블에 입력

            bool result = await accountRepository.CreateAccountAsync(userName, email, hashedPassword, salt);
            if (result == false)
            {
                await _databaseContext.RollbackTransactionAsync();

                return false;
            }

            await _databaseContext.CommitTransactionAsync();

            return true;
        }
        catch (Exception)
        {
            await _databaseContext.RollbackTransactionAsync();

            throw;
        }
    }

    public async Task<string> CreateGuestAccountAsync()
    {
        await _databaseContext.BeginTransactionAsync();

        try
        {
            Guid guid = guidGenerator.GenerateGuid(); // 고유 식별자

            // TODO: 기본 데이터 생성, 유저 id 가져와서 guest 테이블에 입력

            long result = await guestRepository.CreateGuestAsync(guid.ToByteArray());
            if (result <= 0)
            {
                await _databaseContext.RollbackTransactionAsync();

                throw new Exception("Failed to create guest account");
            }

            await _databaseContext.CommitTransactionAsync();

            return guid.ToString();
        }
        catch (Exception)
        {
            await _databaseContext.RollbackTransactionAsync();

            throw;
        }
    }
}