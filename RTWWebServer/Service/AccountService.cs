using RTWWebServer.Authentication;
using RTWWebServer.Repository;

namespace RTWWebServer.Service;

public class AccountService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IGuidGenerator guidGenerator,
    ILogger<AccountService> logger
) : IAccountService
{
    public async Task<bool> CreateAccountAsync(string userName, string email, string password)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync();

            string salt = passwordHasher.GenerateSaltValue();
            string hashedPassword = passwordHasher.CalcHashedPassword(password, salt);

            // TODO: 기본 데이터 생성, 유저 id 가져와서 Account 테이블에 입력

            bool result = await unitOfWork.Accounts.CreateAccountAsync(userName, email, hashedPassword, salt);
            if (result == false)
            {
                await unitOfWork.RollbackTransactionAsync();
                return false;
            }

            await unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<string> CreateGuestAccountAsync()
    {
        try
        {
            await unitOfWork.BeginTransactionAsync();
            
            Guid guid = guidGenerator.GenerateGuid(); // 고유 식별자

            // TODO: 기본 데이터 생성, 유저 id 가져와서 guest 테이블에 입력

            long result = await unitOfWork.Guests.CreateGuestAsync(guid.ToByteArray());
            if (result <= 0)
            {
                await unitOfWork.RollbackTransactionAsync();
                throw new Exception("Failed to create guest account");
            }

            await unitOfWork.CommitTransactionAsync();
            return guid.ToString();
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}