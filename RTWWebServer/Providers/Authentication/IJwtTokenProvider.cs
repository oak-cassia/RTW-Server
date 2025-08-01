using RTWWebServer.Enums;
using RTWWebServer.DTOs;

namespace RTWWebServer.Providers.Authentication;

public interface IJwtTokenProvider
{
    string GenerateJwt(long userId, UserRole role, string email);
    string GenerateJwt(long userId, UserRole role, Guid guid);
    bool ValidateJwt(string token);
    JwtTokenInfo? ParseJwtToken(string token);
}