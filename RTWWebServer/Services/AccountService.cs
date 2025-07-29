using NetworkDefinition.ErrorCode;
using RTWWebServer.Data.Entities;
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

            // Account 객체를 미리 생성
            Account account = new Account(userName, email, hashedPassword, salt);
            
            // 생성된 객체를 Repository에 전달
            await accountUnitOfWork.Accounts.CreateAccountAsync(account);

            await accountUnitOfWork.SaveAsync();
            // DB 쿼리 후 ID가 설정되었는지 확인
            if (account.Id <= 0)
            {
                throw new GameException("Failed to create account - ID not generated", WebServerErrorCode.DatabaseError);
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

            // Guest 객체를 미리 생성
            Guest guest = new Guest(guid);
            
            // 생성된 객체를 Repository에 전달
            await accountUnitOfWork.Guests.CreateGuestAsync(guest);

            await accountUnitOfWork.SaveAsync();
            // DB 쿼리 후 ID가 설정되었는지 확인
            if (guest.Id <= 0)
            {
                throw new GameException("Failed to create guest account - ID not generated", WebServerErrorCode.DatabaseError);
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