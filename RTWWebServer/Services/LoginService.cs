using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Enums;

namespace RTWWebServer.Services;

public class LoginService(
    IAccountRepository accountRepository,
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

        Account? account = await accountRepository.FindByEmailAsync(email);
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
        if (!Guid.TryParse(guestGuid, out Guid parsedGuid))
        {
            throw new GameException("Invalid guest GUID format", WebServerErrorCode.InvalidRequestHttpBody);
        }

        Account? guestAccount = await accountRepository.FindByGuidAsync(parsedGuid.ToString());
        if (guestAccount is not { Role: UserRole.Guest })
        {
            throw new GameException("Guest not found", WebServerErrorCode.GuestNotFound);
        }

        // Guest는 항상 Guest role로 JWT 생성 (guid 포함)
        return jwtTokenProvider.GenerateJwt(guestAccount.Id, UserRole.Guest, parsedGuid);
    }
}