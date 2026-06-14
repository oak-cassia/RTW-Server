using RTWWebServer.Enums;

namespace RTWWebServer.Providers.Authentication;

public interface IJwtTokenProvider
{
    string GenerateJwt(long accountId, UserRole role, string email);
    string GenerateJwt(long accountId, UserRole role, Guid guid);
}
