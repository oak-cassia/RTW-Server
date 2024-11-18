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
        return await accountRepository.CreateAccountAsync(userName, email, hashedPassword, salt);
    }


    public async Task<string> CreateGuestAccountAsync()
    {
        var guid = guidGenerator.GenerateGuid();
        
        if (await guestRepository.CreateGuestAsync(guid.ToByteArray()) <= 0)
        {
            return string.Empty;
        }

        return guid.ToString();
    }
}