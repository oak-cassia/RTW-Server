using NetworkDefinition.ErrorCode;
using RTWWebServer.Authentication;
using RTWWebServer.Cache;
using RTWWebServer.Entity;
using RTWWebServer.Repository;
using RTWWebServer.Exceptions;

namespace RTWWebServer.Service;

public class LoginService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IRemoteCache remoteCache,
    IAuthTokenGenerator authTokenGenerator,
    ILogger<LoginService> logger
) : ILoginService
{
    public async Task<string> LoginAsync(string email, string password)
    {
        // 입력 데이터 검증
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new GameException("Email and password are required", WebServerErrorCode.InvalidRequestHttpBody);
        }

        Account? account = await unitOfWork.Accounts.FindByEmailAsync(email);
        if (account == null)
        {
            logger.LogInformation($"Account with email {email} not found");
            throw new GameException("Invalid email", WebServerErrorCode.InvalidEmail);
        }

        string hashedPassword = passwordHasher.CalcHashedPassword(password, account.Salt);
        if (hashedPassword != account.Password)
        {
            logger.LogInformation($"Password for account with email {email} is incorrect");
            throw new GameException("Invalid password", WebServerErrorCode.InvalidPassword);
        }

        // Todo: AuthToken 반환, Redis 저장, UserId 반환
        string authToken = authTokenGenerator.GenerateToken();
        int userId = 1;

        WebServerErrorCode errorCode = await remoteCache.SetAsync(authToken, userId);
        if (errorCode != WebServerErrorCode.Success)
        {
            throw new GameException("Failed to store auth token", WebServerErrorCode.RemoteCacheError);
        }

        return authToken;
    }

    public async Task<string> GuestLoginAsync(string guestGuid)
    {
        // 입력 데이터 검증
        if (string.IsNullOrWhiteSpace(guestGuid))
        {
            throw new GameException("Guest GUID is required", WebServerErrorCode.InvalidRequestHttpBody);
        }

        if (!Guid.TryParse(guestGuid, out Guid parsedGuid))
        {
            throw new GameException("Invalid guest GUID format", WebServerErrorCode.InvalidRequestHttpBody);
        }

        Guest? guest = await unitOfWork.Guests.FindByGuidAsync(parsedGuid.ToByteArray());
        if (guest == null)
        {
            logger.LogInformation($"Guest with guid {guestGuid} not found");
            throw new GameException("Guest not found", WebServerErrorCode.GuestNotFound);
        }

        // Todo: AuthToken 반환, Redis 저장, UserId 반환
        string authToken = authTokenGenerator.GenerateToken();
        int userId = 1;

        WebServerErrorCode errorCode = await remoteCache.SetAsync(authToken, userId);
        if (errorCode != WebServerErrorCode.Success)
        {
            throw new GameException("Failed to store auth token", WebServerErrorCode.RemoteCacheError);
        }

        return authToken;
    }
}