using System.Security.Cryptography;

namespace RTWWebServer.Authentication;

public class AuthTokenGenerator : IAuthTokenGenerator
{
    public string GenerateToken()
    {
        using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
        byte[] token = new byte[32];
        randomNumberGenerator.GetBytes(token);

        return Convert.ToBase64String(token);
    }
}