using RTWWebServer.Enums;

namespace RTWWebServer.Providers.Authentication;

public interface IJwtTokenProvider
{
    string GenerateJwt(long userId, UserRole role, string email);
    string GenerateJwt(long userId, UserRole role, Guid guid);
    bool ValidateJwt(string token);
    long? GetUserIdFromJwt(string token);
    UserRole? GetUserRoleFromJwt(string token);
    string? GetEmailFromJwt(string token);
    Guid? GetGuidFromJwt(string token);
}