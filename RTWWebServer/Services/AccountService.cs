using NetworkDefinition.ErrorCode;
using RTWWebServer.Data.Repositories;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.Authentication;

namespace RTWWebServer.Services;

public class AccountService(
    IAccountUnitOfWork accountUnitOfWork,
    IPasswordHasher passwordHasher,
    IGuidGenerator guidGenerator
) : IAccountService
{
    public async Task CreateAccountAsync(string userName, string email, string password)
    {
        // 입력 데이터 검증
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new GameException("Email and password are required", WebServerErrorCode.InvalidRequestHttpBody);
        }

        await accountUnitOfWork.BeginTransactionAsync();

        try
        {
            string salt = passwordHasher.GenerateSaltValue();
            string hashedPassword = passwordHasher.CalcHashedPassword(password, salt);

            bool result = await accountUnitOfWork.Accounts.CreateAccountAsync(userName, email, hashedPassword, salt);
            if (!result)
            {
                throw new GameException("Failed to create account", WebServerErrorCode.DatabaseError);
            }

            await accountUnitOfWork.CommitTransactionAsync();
        }
        catch (GameException)
        {
            await accountUnitOfWork.RollbackTransactionAsync();
            throw; // 게임 예외는 그대로 전파
        }
        catch (Exception ex)
        {
            await accountUnitOfWork.RollbackTransactionAsync();
            throw new GameException($"Unexpected error during account creation: {ex.Message}", WebServerErrorCode.InternalServerError);
        }
    }

    public async Task<string> CreateGuestAccountAsync()
    {
        await accountUnitOfWork.BeginTransactionAsync();

        try
        {
            Guid guid = guidGenerator.GenerateGuid(); // 고유 식별자

            long result = await accountUnitOfWork.Guests.CreateGuestAsync(guid.ToByteArray());
            if (result <= 0)
            {
                throw new GameException("Failed to create guest account", WebServerErrorCode.DatabaseError);
            }

            await accountUnitOfWork.CommitTransactionAsync();
            return guid.ToString();
        }
        catch (GameException)
        {
            await accountUnitOfWork.RollbackTransactionAsync();
            throw; // 게임 예외는 그대로 전파
        }
        catch (Exception ex)
        {
            await accountUnitOfWork.RollbackTransactionAsync();
            throw new GameException($"Unexpected error during guest account creation: {ex.Message}", WebServerErrorCode.InternalServerError);
        }
    }
}