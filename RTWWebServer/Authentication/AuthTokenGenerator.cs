using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RTWWebServer.Authentication;

public class AuthTokenGenerator(IConfiguration configuration) : IAuthTokenGenerator
{
    private const string ROLE_CLAIM_TYPE = "role";
    private const string DEFAULT_USER_ROLE = "user";
    private const int TOKEN_EXPIRATION_MINUTES = 30;

    public string GenerateToken()
    {
        using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
        var token = new byte[32];
        randomNumberGenerator.GetBytes(token);

        return Convert.ToBase64String(token);
    }

    public string GenerateJwt(long userId)
    {
        string? secretKey = configuration["Jwt:Secret"];
        SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        Claim[] claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ROLE_CLAIM_TYPE, DEFAULT_USER_ROLE),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(TOKEN_EXPIRATION_MINUTES),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
        };

        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken? token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}