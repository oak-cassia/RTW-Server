using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.Authentication;

namespace RTWWebServer.Services;

public class LoginService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICacheManager cacheManager,
    IJwtTokenProvider jwtTokenProvider,
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
            throw new GameException("Invalid email", WebServerErrorCode.InvalidEmail);
        }

        string hashedPassword = passwordHasher.CalcHashedPassword(password, account.Salt);
        if (hashedPassword != account.Password)
        {
            throw new GameException("Invalid password", WebServerErrorCode.InvalidPassword);
        }

        return jwtTokenProvider.GenerateJwt(account.Id);
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
            throw new GameException("Guest not found", WebServerErrorCode.GuestNotFound);
        }

        //TODO: AuthToken 생성, 캐시에 저장, UserId 반환, Role을 정하고 입장시에 처음 데이터 생성하면 될 듯
        string authToken = jwtTokenProvider.GenerateToken();
        long userId = guest.Id;

        cacheManager.Set(authToken, userId);
        await cacheManager.CommitAllChangesAsync();

        return authToken;
    }
}