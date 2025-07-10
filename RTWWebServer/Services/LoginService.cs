using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Enums;

namespace RTWWebServer.Services;

public class LoginService(
    IAccountUnitOfWork accountUnitOfWork,
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

        Account? account = await accountUnitOfWork.Accounts.FindByEmailAsync(email);
        if (account == null)
        {
            throw new GameException("Invalid email", WebServerErrorCode.InvalidEmail);
        }

        string hashedPassword = passwordHasher.CalcHashedPassword(password, account.Salt);
        if (hashedPassword != account.Password)
        {
            throw new GameException("Invalid password", WebServerErrorCode.InvalidPassword);
        }

        // Account의 role에 따라 JWT 생성 (email 포함)
        return jwtTokenProvider.GenerateJwt(account.Id, account.Role, account.Email);
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

        Guest? guest = await accountUnitOfWork.Guests.FindByGuidAsync(parsedGuid.ToByteArray());
        if (guest == null)
        {
            throw new GameException("Guest not found", WebServerErrorCode.GuestNotFound);
        }

        // Guest는 항상 Guest role로 JWT 생성 (guid 포함)
        return jwtTokenProvider.GenerateJwt(guest.Id, UserRole.Guest, parsedGuid);
    }
}