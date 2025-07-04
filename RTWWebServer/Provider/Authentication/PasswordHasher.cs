using System.Security.Cryptography;
using System.Text;

namespace RTWWebServer.Provider.Authentication;

public class PasswordHasher : IPasswordHasher
{
    private const int STRETCH_COUNT = 2;
    private const int SALT_BYTE_SIZE = 32;

    public string GenerateSaltValue()
    {
        using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();

        var saltValue = new byte[SALT_BYTE_SIZE];
        randomNumberGenerator.GetBytes(saltValue);

        return Convert.ToBase64String(saltValue);
    }

    public string CalcHashedPassword(string password, string salt)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
        using SHA256 sha256Hash = SHA256.Create();

        for (var i = 0; i < STRETCH_COUNT; i++)
        {
            passwordBytes = sha256Hash.ComputeHash(passwordBytes);
        }

        return Convert.ToBase64String(passwordBytes);
    }
}