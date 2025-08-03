using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RTWWebServer.DTOs;
using RTWWebServer.Enums;

namespace RTWWebServer.Providers.Authentication;

public class JwtTokenProvider : IJwtTokenProvider
{
    private const int TOKEN_EXPIRATION_MINUTES = 30;

    // JWT 클레임 타입 상수들 - 표준 ClaimTypes를 상수로 관리
    private const string ACCOUNT_ID_CLAIM_TYPE = "AccountId";
    private const string GUID_CLAIM_TYPE = "Guid";
    private const string EMAIL_CLAIM_TYPE = ClaimTypes.Email;
    private const string ROLE_CLAIM_TYPE = ClaimTypes.Role;
    private const string JTI_CLAIM_TYPE = JwtRegisteredClaimNames.Jti;
    private const string EXP_CLAIM_TYPE = JwtRegisteredClaimNames.Exp;

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

    public string GenerateJwt(long accountId, UserRole role, string email)
    {
        List<Claim> claims =
        [
            new Claim(ACCOUNT_ID_CLAIM_TYPE, accountId.ToString()),
            new Claim(EMAIL_CLAIM_TYPE, email),
            new Claim(JTI_CLAIM_TYPE, Guid.NewGuid().ToString()),
            new Claim(ROLE_CLAIM_TYPE, role.ToString())
        ];
        return GenerateTokenFromClaims(claims);
    }

    public string GenerateJwt(long accountId, UserRole role, Guid guid)
    {
        List<Claim> claims =
        [
            new Claim(ACCOUNT_ID_CLAIM_TYPE, accountId.ToString()),
            new Claim(GUID_CLAIM_TYPE, guid.ToString()),
            new Claim(JTI_CLAIM_TYPE, Guid.NewGuid().ToString()),
            new Claim(ROLE_CLAIM_TYPE, role.ToString())
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

        var tokenInfo = new JwtTokenInfo
        {
            AccountId = GetClaimAsLong(principal, ACCOUNT_ID_CLAIM_TYPE),
            UserRole = GetClaimAsEnum(principal, ROLE_CLAIM_TYPE),
            Email = principal.FindFirst(EMAIL_CLAIM_TYPE)?.Value,
            Guid = GetClaimAsGuid(principal, GUID_CLAIM_TYPE),
            ExpiresAt = GetClaimAsDateTime(principal, EXP_CLAIM_TYPE),
            IsValid = true
        };

        return tokenInfo;
    }

    private long GetClaimAsLong(ClaimsPrincipal principal, string claimType)
    {
        string? claim = principal.FindFirst(claimType)?.Value;
        return long.TryParse(claim, out long result)
            ? result
            : 0;
    }

    private UserRole GetClaimAsEnum(ClaimsPrincipal principal, string claimType)
    {
        string? roleClaim = principal.FindFirst(claimType)?.Value;
        return Enum.TryParse(roleClaim, true, out UserRole role)
            ? role
            : UserRole.Normal;
    }

    private Guid? GetClaimAsGuid(ClaimsPrincipal principal, string claimType)
    {
        string? guidClaim = principal.FindFirst(claimType)?.Value;
        return Guid.TryParse(guidClaim, out Guid guid)
            ? guid
            : null;
    }

    private DateTime? GetClaimAsDateTime(ClaimsPrincipal principal, string claimType)
    {
        string? expClaim = principal.FindFirst(claimType)?.Value;
        return long.TryParse(expClaim, out long exp)
            ? DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime
            : null;
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