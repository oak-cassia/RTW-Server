using RTWWebServer.Authentication;
using RTWWebServer.Database;
using RTWWebServer.Database.Repository;

namespace RTWWebServer.Service;

public class AccountService(
    IMySqlConnectionProvider mySqlConnectionProvider,
    IAccountRepository accountRepository,
    IGuestRepository guestRepository,
    IPasswordHasher passwordHasher,
    IGuidGenerator guidGenerator,
    ILogger<LoginService> logger
) : IAccountService
{
    public async Task<bool> CreateAccountAsync(string userName, string email, string password)
    {
        var connection = await mySqlConnectionProvider.GetAccountConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var salt = passwordHasher.GenerateSaltValue();
            var hashedPassword = passwordHasher.CalcHashedPassword(password, salt);

            // TODO: 기본 데이터 생성, 유저 id 가져와서 Account 테이블에 입력

            var result = await accountRepository.CreateAccountAsync(userName, email, hashedPassword, salt);
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
        var connection = await mySqlConnectionProvider.GetAccountConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var guid = guidGenerator.GenerateGuid();

            // TODO: 기본 데이터 생성, 유저 id 가져와서 guest 테이블에 입력

            var result = await guestRepository.CreateGuestAsync(guid.ToByteArray());
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