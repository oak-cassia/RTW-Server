using NetworkDefinition.ErrorCode;
using RTWWebServer.Authentication;
using RTWWebServer.Database.Repository;

namespace RTWWebServer.Service;

public class LoginService(
    IAccountRepository accountRepository,
    IPasswordHasher passwordHasher,
    IGuestRepository guestRepository,
    IAuthTokenGenerator authTokenGenerator,
    ILogger<LoginService> logger
) : ILoginService
{
    public async Task<(WebServerErrorCode errorCode, string authToken)> LoginAsync(string email, string password)
    {
        var account = await accountRepository.FindByEmailAsync(email);
        if (account == null)
        {
            logger.LogInformation($"Account with email {email} not found");
            return (WebServerErrorCode.InvalidEmail, string.Empty);
        }

        var hashedPassword = passwordHasher.CalcHashedPassword(password, account.Salt);
        if (hashedPassword != account.Password)
        {
            logger.LogInformation($"Password for account with email {email} is incorrect");
            return (WebServerErrorCode.InvalidPassword, string.Empty);
        }

        // Todo: AuthToken 반환, Redis 저장, UserId 반환
        var authToken = authTokenGenerator.GenerateToken();

        return (WebServerErrorCode.Success, authToken);
    }

    public async Task<(WebServerErrorCode errorCode, string authToken)> GuestLoginAsync(string guestGuid)
    {
        var guest = await guestRepository.FindByGuidAsync(Guid.Parse(guestGuid).ToByteArray());
        if (guest == null)
        {
            logger.LogInformation($"Guest with guid {guestGuid} not found");
            return (WebServerErrorCode.GuestNotFound, string.Empty);
        }

        // Todo: AuthToken 반환, Redis 저장, UserId 반환
        var authToken = authTokenGenerator.GenerateToken();

        return (WebServerErrorCode.Success, authToken);
    }
}