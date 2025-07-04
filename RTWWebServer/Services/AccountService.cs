using NetworkDefinition.ErrorCode;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Repositories;

namespace RTWWebServer.Services;

public class AccountService(
    IUnitOfWork unitOfWork,
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

        await unitOfWork.BeginTransactionAsync();

        try
        {
            string salt = passwordHasher.GenerateSaltValue();
            string hashedPassword = passwordHasher.CalcHashedPassword(password, salt);

            // TODO: 기본 데이터 생성, 유저 id 가져와서 Account 테이블에 입력

            bool result = await unitOfWork.Accounts.CreateAccountAsync(userName, email, hashedPassword, salt);
            if (!result)
            {
                throw new GameException("Failed to create account", WebServerErrorCode.DatabaseError);
            }

            await unitOfWork.CommitTransactionAsync();
        }
        catch (GameException)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw; // 게임 예외는 그대로 전파
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw new GameException($"Unexpected error during account creation: {ex.Message}", WebServerErrorCode.InternalServerError);
        }
    }

    public async Task<string> CreateGuestAccountAsync()
    {
        await unitOfWork.BeginTransactionAsync();

        try
        {
            Guid guid = guidGenerator.GenerateGuid(); // 고유 식별자

            // TODO: 기본 데이터 생성, 유저 id 가져와서 guest 테이블에 입력

            long result = await unitOfWork.Guests.CreateGuestAsync(guid.ToByteArray());
            if (result <= 0)
            {
                throw new GameException("Failed to create guest account", WebServerErrorCode.DatabaseError);
            }

            await unitOfWork.CommitTransactionAsync();
            return guid.ToString();
        }
        catch (GameException)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw; // 게임 예외는 그대로 전파
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw new GameException($"Unexpected error during guest account creation: {ex.Message}", WebServerErrorCode.InternalServerError);
        }
    }
}