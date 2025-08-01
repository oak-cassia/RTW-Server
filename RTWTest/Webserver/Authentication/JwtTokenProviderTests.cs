using Microsoft.Extensions.Configuration;
using RTWWebServer.Enums;
using RTWWebServer.Providers.Authentication;

namespace RTWTest.Webserver.Authentication;

[TestFixture]
public class JwtTokenProviderTests
{
    private IJwtTokenProvider _jwtTokenProvider;

    [SetUp]
    public void SetUp()
    {
        var configurationData = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "this-is-a-very-secure-secret-key-for-testing-purposes-that-is-long-enough",
            ["Jwt:Issuer"] = "RTWServer",
            ["Jwt:Audience"] = "RTWClient"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        _jwtTokenProvider = new JwtTokenProvider(configuration);
    }

    [Test]
    public void GenerateAndParseJwt_ShouldWork()
    {
        // Arrange
        long userId = 12345;
        string email = "test@example.com";

        // Act
        string jwt = _jwtTokenProvider.GenerateJwt(userId, UserRole.Normal, email);

        // Assert
        Assert.That(jwt, Is.Not.Null.And.Not.Empty);
        
        var tokenInfo = _jwtTokenProvider.ParseJwtToken(jwt);
        Assert.That(tokenInfo?.IsValid, Is.True);
        Assert.That(tokenInfo?.UserId, Is.EqualTo(userId));
        Assert.That(tokenInfo?.UserRole, Is.EqualTo(UserRole.Normal));
        Assert.That(tokenInfo?.Email, Is.EqualTo(email));
    }

    [Test]
    public void GenerateJwtWithGuid_ShouldWork()
    {
        // Arrange
        long userId = 12345;
        var guid = Guid.NewGuid();

        // Act
        string jwt = _jwtTokenProvider.GenerateJwt(userId, UserRole.Guest, guid);

        // Assert
        var tokenInfo = _jwtTokenProvider.ParseJwtToken(jwt);
        Assert.That(tokenInfo?.IsValid, Is.True);
        Assert.That(tokenInfo?.UserId, Is.EqualTo(userId));
        Assert.That(tokenInfo?.Guid, Is.EqualTo(guid));
    }

    [Test]
    public void InvalidToken_ShouldReturnNull()
    {
        // Act & Assert
        Assert.That(_jwtTokenProvider.ValidateJwt("invalid.jwt.token"), Is.False);
        Assert.That(_jwtTokenProvider.ParseJwtToken("invalid.jwt.token"), Is.Null);
    }
}
