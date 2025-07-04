namespace RTWWebServer.Authentication;

public interface IAuthTokenGenerator
{
    string GenerateToken();
    string GenerateJwt(long userId);
    bool ValidateJwt(string token);
    long? GetUserIdFromJwt(string token);
}