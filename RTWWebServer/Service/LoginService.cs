using NetworkDefinition.ErrorCode;
using RTWWebServer.Authentication;
using RTWWebServer.Database.Cache;
using RTWWebServer.Database.Entity;
using RTWWebServer.Database.Repository;

namespace RTWWebServer.Service;

public class LoginService(
    IAccountRepository accountRepository,
    IPasswordHasher passwordHasher,
    IGuestRepository guestRepository,
    IRemoteCache remoteCache,
    IAuthTokenGenerator authTokenGenerator,
    ILogger<LoginService> logger
) : ILoginService
{
    public async Task<(WebServerErrorCode errorCode, string authToken)> LoginAsync(string email, string password)
    {
        Account? account = await accountRepository.FindByEmailAsync(email);
        if (account == null)
        {
            logger.LogInformation($"Account with email {email} not found");
            return (WebServerErrorCode.InvalidEmail, string.Empty);
        }

        string hashedPassword = passwordHasher.CalcHashedPassword(password, account.Salt);
        if (hashedPassword != account.Password)
        {
            logger.LogInformation($"Password for account with email {email} is incorrect");
            return (WebServerErrorCode.InvalidPassword, string.Empty);
        }

        // Todo: AuthToken 반환, Redis 저장, UserId 반환
        string authToken = authTokenGenerator.GenerateToken();
        int userId = 1;

        WebServerErrorCode errorCode = await remoteCache.SetAsync(authToken, userId);

        return (errorCode, authToken);
    }

    public async Task<(WebServerErrorCode errorCode, string authToken)> GuestLoginAsync(string guestGuid)
    {
        Guest? guest = await guestRepository.FindByGuidAsync(Guid.Parse(guestGuid).ToByteArray());
        if (guest == null)
        {
            logger.LogInformation($"Guest with guid {guestGuid} not found");
            return (WebServerErrorCode.GuestNotFound, string.Empty);
        }

        // Todo: AuthToken 반환, Redis 저장, UserId 반환
        string authToken = authTokenGenerator.GenerateToken();
        int userId = 1;

        WebServerErrorCode errorCode = await remoteCache.SetAsync(authToken, userId);

        return (errorCode, authToken);
    }
}