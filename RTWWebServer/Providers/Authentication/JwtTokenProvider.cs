using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RTWWebServer.DTOs;
using RTWWebServer.Enums;

namespace RTWWebServer.Providers.Authentication;

public class JwtTokenProvider : IJwtTokenProvider
{
    private const string GUID_CLAIM_TYPE = "guid";
    private const int TOKEN_EXPIRATION_MINUTES = 30;

    private const string JWT_SECRET_KEY = "Jwt:Secret";
    private const string JWT_ISSUER_KEY = "Jwt:Issuer";
    private const string JWT_AUDIENCE_KEY = "Jwt:Audience";
    private static readonly JwtSecurityTokenHandler TokenHandler = new JwtSecurityTokenHandler();
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
        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),

            new Claim(JwtRegisteredClaimNames.Email, email),

            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new Claim(ClaimTypes.Role, role.ToRoleString())
        ];
        return GenerateTokenFromClaims(claims);
    }

    public string GenerateJwt(long userId, UserRole role, Guid guid)
    {
        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),

            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new Claim(ClaimTypes.Role, role.ToRoleString()),

            new Claim(GUID_CLAIM_TYPE, guid.ToString())
        ];
        return GenerateTokenFromClaims(claims);
    }

    public bool ValidateJwt(string token)
    {
        return GetPrincipalFromToken(token) != null;
    }

    public JwtTokenInfo? ParseJwtToken(string token)
    {
        ClaimsPrincipal? principal = GetPrincipalFromToken(token);
        if (principal == null)
        {
            return null; // Validation failed
        }

        JwtTokenInfo tokenInfo = new JwtTokenInfo();
        
        string? userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(userIdClaim, out long userId))
        {
            tokenInfo.UserId = userId;
        }
        
        string? roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
        if (roleClaim != null)
        {
            tokenInfo.UserRole = UserRoleExtensions.FromRoleString(roleClaim);
        }
        
        tokenInfo.Email = principal.FindFirst(ClaimTypes.Email)?.Value;
        
        string? guidClaim = principal.FindFirst(GUID_CLAIM_TYPE)?.Value;
        if (guidClaim != null && Guid.TryParse(guidClaim, out Guid guid))
        {
            tokenInfo.Guid = guid;
        }

        // JTI 파싱
        tokenInfo.Jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        // Expiration 파싱
        string? expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (long.TryParse(expClaim, out long exp))
        {
            tokenInfo.Expiration = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
        }

        return tokenInfo;
    }

    private string GenerateTokenFromClaims(IEnumerable<Claim> claims)
    {
        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(TOKEN_EXPIRATION_MINUTES),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256Signature)
        };

        SecurityToken? securityToken = TokenHandler.CreateToken(tokenDescriptor);
        return TokenHandler.WriteToken(securityToken);
    }

    private ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        try
        {
            return TokenHandler.ValidateToken(token, _tokenValidationParameters, out _);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }
}