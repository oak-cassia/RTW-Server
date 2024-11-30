using System.Security.Cryptography;
using System.Text;

namespace RTWWebServer.Authentication;

public class PasswordHasher : IPasswordHasher
{
    const int StretchCount = 2;

    public string GenerateSaltValue()
    {
        using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();

        byte[] saltValue = new byte[64];
        randomNumberGenerator.GetBytes(saltValue);

        return Convert.ToBase64String(saltValue);
    }

    public string CalcHashedPassword(string password, string salt)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
        using SHA256 sha256Hash = SHA256.Create();

        for (int i = 0; i < StretchCount; i++)
        {
            passwordBytes = sha256Hash.ComputeHash(passwordBytes);
        }

        return Convert.ToBase64String(passwordBytes);
    }
}