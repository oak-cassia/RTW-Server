using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using RTWWebServer.Authentication;

namespace RTWTest.Webserver;

[TestFixture]
public class AuthTokenGeneratorTests
{
    private Mock<IConfiguration> _mockConfiguration;
    private AuthTokenGenerator _authTokenGenerator;

    [SetUp]
    public void Setup()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        var mockJwtSection = new Mock<IConfigurationSection>();
        mockJwtSection.Setup(x => x.Value).Returns("MySuperLongAndSecureSecretKey123!@#");
        _mockConfiguration.Setup(x => x.GetSection("Jwt:Secret")).Returns(mockJwtSection.Object);

        var mockIssuerSection = new Mock<IConfigurationSection>();
        mockIssuerSection.Setup(x => x.Value).Returns("my-auth-server.com");
        _mockConfiguration.Setup(x => x.GetSection("Jwt:Issuer")).Returns(mockIssuerSection.Object);

        var mockAudienceSection = new Mock<IConfigurationSection>();
        mockAudienceSection.Setup(x => x.Value).Returns("my-game-server.com");
        _mockConfiguration.Setup(x => x.GetSection("Jwt:Audience")).Returns(mockAudienceSection.Object);
        
        _authTokenGenerator = new AuthTokenGenerator(_mockConfiguration.Object);
    }

    [Test]
    public void GenerateJwt_WhenCalled_ReturnsValidJwt()
    {
        // Arrange
        const long userId = 123;

        // Act
        var jwtString = _authTokenGenerator.GenerateJwt(userId);

        // Assert
        Assert.That(jwtString, Is.Not.Null.And.Not.Empty);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwtString);

        Assert.That(token, Is.Not.Null);
        Assert.That(token.Issuer, Is.EqualTo("my-auth-server.com"));
        Assert.That(token.Audiences.First(), Is.EqualTo("my-game-server.com"));
        
        var subClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.That(subClaim, Is.Not.Null);
        Assert.That(subClaim.Value, Is.EqualTo(userId.ToString()));

        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == "role");
        Assert.That(roleClaim, Is.Not.Null);
        Assert.That(roleClaim.Value, Is.EqualTo("user"));
    }
}

