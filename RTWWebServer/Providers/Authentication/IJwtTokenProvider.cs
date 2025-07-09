using RTWWebServer.Enums;

namespace RTWWebServer.Providers.Authentication;

public interface IJwtTokenProvider
{
    string GenerateToken();
    string GenerateJwt(long userId);
    string GenerateJwt(long userId, UserRole role);
    bool ValidateJwt(string token);
    long? GetUserIdFromJwt(string token);
    UserRole? GetUserRoleFromJwt(string token);
}