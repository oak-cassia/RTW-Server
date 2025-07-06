using Microsoft.Extensions.Configuration;
using RTWWebServer.Providers.Authentication;

namespace RTWTest.Webserver.Authentication;

[TestFixture]
public class JwtTokenProviderTests
{
    private IJwtTokenProvider _jwtTokenProvider;

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

        _jwtTokenProvider = new JwtTokenProvider(configuration);
    }

    [Test]
    public void GenerateJwt_ShouldCreateValidTokenWithCorrectStructure()
    {
        // Arrange
        long userId = 12345;

        // Act
        string jwt = _jwtTokenProvider.GenerateJwt(userId);

        // Assert - 기본 JWT 구조 검증
        Assert.That(jwt, Is.Not.Null.And.Not.Empty);
        Assert.That(jwt, Does.Contain(".")); // JWT는 점(.)으로 구분된 구조를 가짐

        // Assert - JWT 토큰 검증
        Assert.That(_jwtTokenProvider.ValidateJwt(jwt), Is.True);

        // Assert - 사용자 ID 추출 검증
        long? extractedUserId = _jwtTokenProvider.GetUserIdFromJwt(jwt);
        Assert.That(extractedUserId, Is.Not.Null);
        Assert.That(extractedUserId.Value, Is.EqualTo(userId));
    }

    [Test]
    public void InvalidToken_ShouldReturnFalseAndNull()
    {
        // Arrange
        string invalidJwt = "invalid.jwt.token";

        // Act & Assert
        Assert.That(_jwtTokenProvider.ValidateJwt(invalidJwt), Is.False);
        Assert.That(_jwtTokenProvider.GetUserIdFromJwt(invalidJwt), Is.Null);
    }

    [Test]
    public void MultipleUsers_ShouldGenerateUniqueValidTokens()
    {
        // Arrange
        long userId1 = 1001;
        long userId2 = 1002;

        // Act
        string jwt1 = _jwtTokenProvider.GenerateJwt(userId1);
        string jwt2 = _jwtTokenProvider.GenerateJwt(userId2);

        // Assert - 토큰이 서로 다름
        Assert.That(jwt1, Is.Not.EqualTo(jwt2));

        // Assert - 두 토큰 모두 유효함
        Assert.That(_jwtTokenProvider.ValidateJwt(jwt1), Is.True);
        Assert.That(_jwtTokenProvider.ValidateJwt(jwt2), Is.True);

        // Assert - 각 토큰에서 올바른 사용자 ID 추출
        Assert.That(_jwtTokenProvider.GetUserIdFromJwt(jwt1), Is.EqualTo(userId1));
        Assert.That(_jwtTokenProvider.GetUserIdFromJwt(jwt2), Is.EqualTo(userId2));
    }

    [Test]
    public void GenerateToken_ShouldCreateUniqueRandomTokens()
    {
        // Act
        string token1 = _jwtTokenProvider.GenerateToken();
        string token2 = _jwtTokenProvider.GenerateToken();

        // Assert
        Assert.That(token1, Is.Not.Null.And.Not.Empty);
        Assert.That(token2, Is.Not.Null.And.Not.Empty);
        Assert.That(token1, Is.Not.EqualTo(token2)); // 매번 다른 토큰이 생성되어야 함
    }
}