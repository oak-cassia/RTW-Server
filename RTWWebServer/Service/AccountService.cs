using NetworkDefinition.ErrorCode;
using RTWWebServer.Authentication;
using RTWWebServer.Database.Repository;

namespace RTWWebServer.Service;

public class AccountService(
    IAccountRepository accountRepository,
    IGuestRepository guestRepository,
    IPasswordHasher passwordHasher,
    IGuidGenerator guidGenerator,
    ILogger<LoginService> logger
) : IAccountService
{
    public async Task<bool> CreateAccountAsync(string userName, string email, string password)
    {
        var salt = passwordHasher.GenerateSaltValue();
        var hashedPassword = passwordHasher.CalcHashedPassword(password, salt);

        // TODO: 기본 데이터 생성, 유저 id 가져와서 Account 테이블에 입력

        return await accountRepository.CreateAccountAsync(userName, email, hashedPassword, salt);
    }


    public async Task<string> CreateGuestAccountAsync()
    {
        var guid = guidGenerator.GenerateGuid();
        if (await guestRepository.CreateGuestAsync(guid.ToByteArray()) <= 0)
        {
            return string.Empty;
        }

        // TODO: 기본 데이터 생성, 유저 id 가져와서 guest 테이블에 입력

        return guid.ToString();
    }
}