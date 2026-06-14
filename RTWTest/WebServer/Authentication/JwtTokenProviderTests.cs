using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using RTWWebServer.Enums;
using RTWWebServer.Providers.Authentication;

namespace RTWTest.WebServer.Authentication;

// 토큰 '검증'은 운영에서 JwtBearer 미들웨어가 담당하므로, 여기서는 발급(GenerateJwt)이
// 올바른 클레임·만료시각을 박는지만 가드한다. 검증 경로는 더 이상 JwtTokenProvider에 없다.
[TestFixture]
public class JwtTokenProviderTests
{
    [SetUp]
    public void SetUp()
    {
        Dictionary<string, string?> configurationData = new()
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
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private const string TEST_SECRET = "this-is-a-very-secure-secret-key-for-testing-purposes-that-is-long-enough";
    private const string TEST_ISSUER = "RTWServer";
    private const string TEST_AUDIENCE = "RTWClient";

    [Test]
    public void GenerateJwt_WithEmail_EmbedsExpectedClaims()
    {
        long accountId = 12345;
        var email = "test@example.com";

        string jwt = _jwtTokenProvider.GenerateJwt(accountId, UserRole.Normal, email);

        Assert.That(jwt, Is.Not.Null.And.Not.Empty);
        JwtSecurityToken token = _tokenHandler.ReadJwtToken(jwt);
        Assert.Multiple(() =>
        {
            Assert.That(GetClaim(token, JwtRegisteredClaimNames.Sub), Is.EqualTo(accountId.ToString()));
            Assert.That(GetClaim(token, JwtRegisteredClaimNames.Email), Is.EqualTo(email));
            Assert.That(GetClaim(token, "role"), Is.EqualTo(UserRole.Normal.ToString()));
            Assert.That(token.Issuer, Is.EqualTo(TEST_ISSUER));
            Assert.That(token.Audiences, Does.Contain(TEST_AUDIENCE));
        });
    }

    [Test]
    public void GenerateJwt_WithGuid_EmbedsExpectedClaims()
    {
        long accountId = 12345;
        Guid guid = Guid.NewGuid();

        string jwt = _jwtTokenProvider.GenerateJwt(accountId, UserRole.Guest, guid);

        JwtSecurityToken token = _tokenHandler.ReadJwtToken(jwt);
        Assert.Multiple(() =>
        {
            Assert.That(GetClaim(token, JwtRegisteredClaimNames.Sub), Is.EqualTo(accountId.ToString()));
            Assert.That(GetClaim(token, "Guid"), Is.EqualTo(guid.ToString()));
            Assert.That(GetClaim(token, "role"), Is.EqualTo(UserRole.Guest.ToString()));
        });
    }

    [Test]
    public void GenerateJwt_SetsThirtyMinuteExpiration()
    {
        string jwt = _jwtTokenProvider.GenerateJwt(12345, UserRole.Normal, "test@example.com");

        JwtSecurityToken token = _tokenHandler.ReadJwtToken(jwt);
        Assert.Multiple(() =>
        {
            Assert.That(token.ValidTo, Is.GreaterThan(DateTime.UtcNow));
            Assert.That(token.ValidTo, Is.LessThan(DateTime.UtcNow.AddMinutes(31))); // 30분 + 여유시간
        });
    }

    private static string? GetClaim(JwtSecurityToken token, string type)
    {
        return token.Claims.FirstOrDefault(c => c.Type == type)?.Value;
    }
}
