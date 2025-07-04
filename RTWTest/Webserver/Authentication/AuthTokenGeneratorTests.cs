using Microsoft.Extensions.Configuration;
using RTWWebServer.Authentication;

namespace RTWTest.Webserver.Authentication;

[TestFixture]
public class AuthTokenGeneratorTests
{
    private IAuthTokenGenerator _authTokenGenerator;

    [SetUp]
    public void SetUp()
    {
        // 테스트용 Configuration 설정
        var configurationData = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "this-is-a-very-secure-secret-key-for-testing-purposes-that-is-long-enough",
            ["Jwt:Issuer"] = "RTWServer",
            ["Jwt:Audience"] = "RTWClient"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        _authTokenGenerator = new AuthTokenGenerator(configuration);
    }

    [Test]
    public void GenerateJwt_ShouldCreateValidTokenWithCorrectStructure()
    {
        // Arrange
        long userId = 12345;

        // Act
        string jwt = _authTokenGenerator.GenerateJwt(userId);

        // Assert - 기본 JWT 구조 검증
        Assert.That(jwt, Is.Not.Null.And.Not.Empty);
        Assert.That(jwt, Does.Contain(".")); // JWT는 점(.)으로 구분된 구조를 가짐

        // Assert - JWT 토큰 검증
        Assert.That(_authTokenGenerator.ValidateJwt(jwt), Is.True);

        // Assert - 사용자 ID 추출 검증
        long? extractedUserId = _authTokenGenerator.GetUserIdFromJwt(jwt);
        Assert.That(extractedUserId, Is.Not.Null);
        Assert.That(extractedUserId.Value, Is.EqualTo(userId));
    }


    [Test]
    public void InvalidToken_ShouldReturnFalseAndNull()
    {
        // Arrange
        string invalidJwt = "invalid.jwt.token";

        // Act & Assert
        Assert.That(_authTokenGenerator.ValidateJwt(invalidJwt), Is.False);
        Assert.That(_authTokenGenerator.GetUserIdFromJwt(invalidJwt), Is.Null);
    }

    [Test]
    public void MultipleUsers_ShouldGenerateUniqueValidTokens()
    {
        // Arrange
        long userId1 = 1001;
        long userId2 = 1002;

        // Act
        string jwt1 = _authTokenGenerator.GenerateJwt(userId1);
        string jwt2 = _authTokenGenerator.GenerateJwt(userId2);

        // Assert - 토큰이 서로 다름
        Assert.That(jwt1, Is.Not.EqualTo(jwt2));

        // Assert - 두 토큰 모두 유효함
        Assert.That(_authTokenGenerator.ValidateJwt(jwt1), Is.True);
        Assert.That(_authTokenGenerator.ValidateJwt(jwt2), Is.True);

        // Assert - 각 토큰에서 올바른 사용자 ID 추출
        Assert.That(_authTokenGenerator.GetUserIdFromJwt(jwt1), Is.EqualTo(userId1));
        Assert.That(_authTokenGenerator.GetUserIdFromJwt(jwt2), Is.EqualTo(userId2));
    }
}