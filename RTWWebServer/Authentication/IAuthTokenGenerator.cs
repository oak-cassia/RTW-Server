namespace RTWWebServer.Authentication;

public interface IAuthTokenGenerator
{
    string GenerateToken();
}