using RTWWebServer.DTOs;
using RTWWebServer.Enums;

namespace RTWWebServer.Providers.Authentication;

public interface IJwtTokenProvider
{
    string GenerateJwt(long accountId, UserRole role, string email);
    string GenerateJwt(long accountId, UserRole role, Guid guid);
    bool ValidateJwt(string token);
    JwtTokenInfo? ParseJwtToken(string token);
}