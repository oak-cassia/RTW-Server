using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RTWWebServer.Enums;

namespace RTWWebServer.Providers.Authentication;

public class JwtTokenProvider : IJwtTokenProvider
{
    private const int TOKEN_EXPIRATION_MINUTES = 30;

    // JWT 클레임 타입 상수들 - 표준 ClaimTypes를 상수로 관리
    private const string GUID_CLAIM_TYPE = "Guid";
    private const string EMAIL_CLAIM_TYPE = JwtRegisteredClaimNames.Email;
    private const string ROLE_CLAIM_TYPE = "role";
    private const string JTI_CLAIM_TYPE = JwtRegisteredClaimNames.Jti;

    private const string JWT_SECRET_KEY = "Jwt:Secret";
    private const string JWT_ISSUER_KEY = "Jwt:Issuer";
    private const string JWT_AUDIENCE_KEY = "Jwt:Audience";
    private static readonly JwtSecurityTokenHandler TokenHandler = new JwtSecurityTokenHandler();
    private readonly string _audience;
    private readonly string _issuer;

    private readonly SymmetricSecurityKey _securityKey;

    public JwtTokenProvider(IConfiguration configuration)
    {
        _issuer = configuration[JWT_ISSUER_KEY]!;
        _audience = configuration[JWT_AUDIENCE_KEY]!;
        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration[JWT_SECRET_KEY]!));
    }

    public string GenerateJwt(long accountId, UserRole role, string email)
    {
        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()), // 표준 subject claim
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
            new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()), // 표준 subject claim
            new Claim(GUID_CLAIM_TYPE, guid.ToString()),
            new Claim(JTI_CLAIM_TYPE, Guid.NewGuid().ToString()),
            new Claim(ROLE_CLAIM_TYPE, role.ToString())
        ];
        return GenerateTokenFromClaims(claims);
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
}
