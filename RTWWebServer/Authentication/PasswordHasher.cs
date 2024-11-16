using System.Security.Cryptography;
using System.Text;

namespace RTWWebServer.Authentication;

public class PasswordHasher : IPasswordHasher
{
    const int StretchCount = 2;

    public string GetNewSalt()
    {
        using (var randomNumberGenerator = RandomNumberGenerator.Create())
        {
            var saltValue = new byte[64];
            randomNumberGenerator.GetBytes(saltValue);

            return Convert.ToBase64String(saltValue);
        }
    }

    public string CalcHashedPassword(string password, string salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password + salt);
        using (var sha256Hash = SHA256.Create())
        {
            for (var i = 0; i < StretchCount; i++) passwordBytes = sha256Hash.ComputeHash(passwordBytes);

            return Convert.ToBase64String(passwordBytes);
        }
    }
}