using System.Security.Cryptography;
using System.Text;

namespace RTWWebServer.Providers.Authentication;

public class PasswordHasher : IPasswordHasher
{
    private const int SALT_BYTE_SIZE = 32;
    private const int HASH_BYTE_SIZE = 32;

    // PBKDF2 반복 횟수. 빠른 해시(SHA-256 단순 반복)의 무차별 대입 취약점을 막기 위한 키 강화.
    private const int PBKDF2_ITERATIONS = 100_000;

    public string GenerateSaltValue()
    {
        using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();

        var saltValue = new byte[SALT_BYTE_SIZE];
        randomNumberGenerator.GetBytes(saltValue);

        return Convert.ToBase64String(saltValue);
    }

    public string CalcHashedPassword(string password, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            saltBytes,
            PBKDF2_ITERATIONS,
            HashAlgorithmName.SHA256,
            HASH_BYTE_SIZE);

        return Convert.ToBase64String(hash);
    }
}