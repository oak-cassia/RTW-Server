using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RTWWebServer.Enums;

namespace RTWWebServer.Providers.Authentication;

public class JwtTokenProvider : IJwtTokenProvider
{
    private const string ROLE_CLAIM_TYPE = "role";
    private const int TOKEN_EXPIRATION_MINUTES = 30;

    private const string JWT_SECRET_KEY = "Jwt:Secret";
    private const string JWT_ISSUER_KEY = "Jwt:Issuer";
    private const string JWT_AUDIENCE_KEY = "Jwt:Audience";
    private readonly string _audience;
    private readonly string _issuer;

    private readonly SymmetricSecurityKey _securityKey;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtTokenProvider(IConfiguration configuration)
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

    public string GenerateJwt(long userId, UserRole role, string email)
    {
        Claim[] claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ROLE_CLAIM_TYPE, role.ToRoleString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email)
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

    public string GenerateJwt(long userId, UserRole role, Guid guid)
    {
        Claim[] claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ROLE_CLAIM_TYPE, role.ToRoleString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("guid", guid.ToString())
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

    public UserRole? GetUserRoleFromJwt(string token)
    {
        try
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            JwtSecurityToken jwt = tokenHandler.ReadJwtToken(token);
            string? roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ROLE_CLAIM_TYPE)?.Value;

            return roleClaim != null
                ? UserRoleExtensions.FromRoleString(roleClaim)
                : null;
        }
        catch
        {
            return null;
        }
    }

    public string? GetEmailFromJwt(string token)
    {
        try
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            JwtSecurityToken jwt = tokenHandler.ReadJwtToken(token);
            return jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
        }
        catch
        {
            return null;
        }
    }

    public Guid? GetGuidFromJwt(string token)
    {
        try
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            JwtSecurityToken jwt = tokenHandler.ReadJwtToken(token);
            string? guidClaim = jwt.Claims.FirstOrDefault(c => c.Type == "guid")?.Value;

            return guidClaim != null && Guid.TryParse(guidClaim, out Guid guid)
                ? guid
                : null;
        }
        catch
        {
            return null;
        }
    }
}