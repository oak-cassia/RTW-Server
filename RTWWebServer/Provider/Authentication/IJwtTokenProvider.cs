namespace RTWWebServer.Provider.Authentication;

public interface IJwtTokenProvider
{
    string GenerateToken();
    string GenerateJwt(long userId);
    bool ValidateJwt(string token);
    long? GetUserIdFromJwt(string token);
}