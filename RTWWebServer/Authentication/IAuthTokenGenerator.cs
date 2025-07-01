namespace RTWWebServer.Authentication;

public interface IAuthTokenGenerator
{
    string GenerateToken();
    string GenerateJwt(long userId);
}