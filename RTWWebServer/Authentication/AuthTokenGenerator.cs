using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RTWWebServer.Authentication;

public class AuthTokenGenerator : IAuthTokenGenerator
{
    private const string ROLE_CLAIM_TYPE = "role";
    private const string DEFAULT_USER_ROLE = "user";
    private const int TOKEN_EXPIRATION_MINUTES = 30;

    private const string JWT_SECRET_KEY = "Jwt:Secret";
    private const string JWT_ISSUER_KEY = "Jwt:Issuer";
    private const string JWT_AUDIENCE_KEY = "Jwt:Audience";

    private readonly SymmetricSecurityKey _securityKey;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly string _issuer;
    private readonly string _audience;

    public AuthTokenGenerator(IConfiguration configuration)
    {
        _issuer = configuration[JWT_ISSUER_KEY]!;
        _audience = configuration[JWT_AUDIENCE_KEY]!;
        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration[JWT_SECRET_KEY]!));
        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _securityKey,
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public string GenerateToken()
    {
        using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
        var token = new byte[32];
        randomNumberGenerator.GetBytes(token);

        return Convert.ToBase64String(token);
    }

    public string GenerateJwt(long userId)
    {
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
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256Signature)
        };

        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken? token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateJwt(string token)
    {
        try
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, _tokenValidationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public long? GetUserIdFromJwt(string token)
    {
        try
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            JwtSecurityToken jwt = tokenHandler.ReadJwtToken(token);
            string? userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            return long.TryParse(userIdClaim, out long userId)
                ? userId
                : null;
        }
        catch
        {
            return null;
        }
    }
}