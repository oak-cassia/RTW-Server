using System.Security.Cryptography;
using System.Text;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Enums;

namespace RTWWebServer.Services;

public class LoginService(
    IAccountRepository accountRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenProvider jwtTokenProvider
) : ILoginService
{
    public async Task<string> LoginAsync(string email, string password)
    {
        // 입력 데이터 검증
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new GameException("Email and password are required", WebServerErrorCode.InvalidRequestHttpBody);
        }

        // 이메일 존재 여부와 비밀번호 불일치를 구분해서 응답하면 계정 열거(enumeration)가 가능하므로
        // 모든 실패 경로에서 동일한 에러 코드를 반환한다.
        // Password/Salt가 null인 계정(게스트 계정)은 이메일/비밀번호 로그인 대상이 아니다.
        Account? account = await accountRepository.FindByEmailAsync(email);
        if (account?.Password == null || account.Salt == null || account.Email == null)
        {
            throw new GameException("Invalid email or password", WebServerErrorCode.InvalidCredentials);
        }

        string hashedPassword = passwordHasher.CalcHashedPassword(password, account.Salt);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hashedPassword),
                Encoding.UTF8.GetBytes(account.Password)))
        {
            throw new GameException("Invalid email or password", WebServerErrorCode.InvalidCredentials);
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