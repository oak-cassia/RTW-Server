using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RTWWebServer.DTOs;
using RTWWebServer.Enums;
using RTWWebServer.Providers.Authentication;

namespace RTWTest.Webserver.Authentication;

[TestFixture]
public class JwtTokenProviderTests
{
    [SetUp]
    public void SetUp()
    {
        Dictionary<string, string?> configurationData = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = TEST_SECRET,
            ["Jwt:Issuer"] = TEST_ISSUER,
            ["Jwt:Audience"] = TEST_AUDIENCE
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        _jwtTokenProvider = new JwtTokenProvider(configuration);
    }

    private IJwtTokenProvider _jwtTokenProvider;
    private const string TEST_SECRET = "this-is-a-very-secure-secret-key-for-testing-purposes-that-is-long-enough";
    private const string TEST_ISSUER = "RTWServer";
    private const string TEST_AUDIENCE = "RTWClient";

    [Test]
    public void GenerateAndParseJwt_WithEmail_ShouldWork()
    {
        // Arrange
        long accountId = 12345;
        var email = "test@example.com";

        // Act
        string jwt = _jwtTokenProvider.GenerateJwt(accountId, UserRole.Normal, email);
        JwtTokenInfo? tokenInfo = _jwtTokenProvider.ParseJwtToken(jwt);

        // Assert
        Assert.That(jwt, Is.Not.Null.And.Not.Empty);
        Assert.That(tokenInfo?.IsValid, Is.True);
        Assert.That(tokenInfo?.AccountId, Is.EqualTo(accountId));
        Assert.That(tokenInfo?.UserRole, Is.EqualTo(UserRole.Normal));
        Assert.That(tokenInfo?.Email, Is.EqualTo(email));
    }

    [Test]
    public void GenerateAndParseJwt_WithGuid_ShouldWork()
    {
        // Arrange
        long accountId = 12345;
        Guid guid = Guid.NewGuid();

        // Act
        string jwt = _jwtTokenProvider.GenerateJwt(accountId, UserRole.Guest, guid);
        JwtTokenInfo? tokenInfo = _jwtTokenProvider.ParseJwtToken(jwt);

        // Assert
        Assert.That(tokenInfo?.IsValid, Is.True);
        Assert.That(tokenInfo?.AccountId, Is.EqualTo(accountId));
        Assert.That(tokenInfo?.UserRole, Is.EqualTo(UserRole.Guest));
        Assert.That(tokenInfo?.Guid, Is.EqualTo(guid));
    }

    [Test]
    public void InvalidToken_ShouldReturnFalseAndNull()
    {
        // Act & Assert
        Assert.That(_jwtTokenProvider.ValidateJwt("invalid.jwt.token"), Is.False);
        Assert.That(_jwtTokenProvider.ParseJwtToken("invalid.jwt.token"), Is.Null);
    }

    [Test]
    public void JwtTokenExpiration_ShouldBeValid()
    {
        // Arrange
        long accountId = 12345;
        var email = "test@example.com";

        // Act
        string jwt = _jwtTokenProvider.GenerateJwt(accountId, UserRole.Normal, email);
        JwtTokenInfo? tokenInfo = _jwtTokenProvider.ParseJwtToken(jwt);

        // Assert
        Assert.That(tokenInfo?.ExpiresAt, Is.Not.Null);
        Assert.That(tokenInfo?.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
        Assert.That(tokenInfo?.ExpiresAt, Is.LessThan(DateTime.UtcNow.AddMinutes(31))); // 30분 + 여유시간
    }

    [Test]
    public void ExpiredToken_ShouldFailValidation()
    {
        // Arrange - 만료된 토큰 생성
        string expiredJwt = CreateExpiredToken(12345, "test@example.com");

        // Act & Assert
        Assert.That(_jwtTokenProvider.ValidateJwt(expiredJwt), Is.False);
    }

    private string CreateExpiredToken(long accountId, string email)
    {
        DateTime pastTime = DateTime.UtcNow.AddMinutes(-15);
        DateTime expiredTime = pastTime.AddMinutes(5);

        List<Claim> claims = new List<Claim>
        {
            new Claim("AccountId", accountId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Normal")
        };

        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = pastTime,
            Expires = expiredTime,
            IssuedAt = pastTime,
            Issuer = TEST_ISSUER,
            Audience = TEST_AUDIENCE,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TEST_SECRET)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken? token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}