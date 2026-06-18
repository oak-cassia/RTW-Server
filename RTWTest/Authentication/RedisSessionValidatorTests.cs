using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using RTWServer.Authentication;
using RTWWebServer.DTOs;

namespace RTWTest.Authentication;

// 게임 서버가 웹 서버 세션(session_{userId})을 직접 조회·검증하는 로직을 검증한다.
// 웹 서버 UserSession을 그대로 직렬화한 바이트를 캐시 응답으로 흘려보내 교차 서비스 포맷 계약을 고정한다.
[TestFixture]
public class RedisSessionValidatorTests
{
    private const long UserId = 42;

    private static RedisSessionValidator CreateValidator(Mock<IDistributedCache> cache)
    {
        var logger = new Mock<ILogger<RedisSessionValidator>>().Object;
        return new RedisSessionValidator(cache.Object, logger);
    }

    // 웹 서버 DistributedCacheAdapter.SetAsync와 동일하게 직렬화한다(System.Text.Json 기본 옵션).
    private static byte[] SerializeWebSession(long userId, string token, string nickname = "Hero")
    {
        var webSession = new UserSession(userId, token, nickname);
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(webSession));
    }

    private static void SetupCache(Mock<IDistributedCache> cache, string key, byte[]? value)
    {
        cache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(value);
    }

    [Test]
    public async Task ValidateAsync_WithMatchingToken_ReturnsTrue()
    {
        var cache = new Mock<IDistributedCache>();
        SetupCache(cache, $"session_{UserId}", SerializeWebSession(UserId, "good-token"));
        var validator = CreateValidator(cache);

        var result = await validator.ValidateAsync(UserId, "good-token");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Nickname, Is.EqualTo("Hero"));
    }

    [Test]
    public async Task ValidateAsync_WithMismatchedToken_ReturnsFalse()
    {
        var cache = new Mock<IDistributedCache>();
        SetupCache(cache, $"session_{UserId}", SerializeWebSession(UserId, "good-token"));
        var validator = CreateValidator(cache);

        var result = await validator.ValidateAsync(UserId, "wrong-token");

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task ValidateAsync_WhenSessionMissing_ReturnsFalse()
    {
        var cache = new Mock<IDistributedCache>();
        SetupCache(cache, $"session_{UserId}", null);
        var validator = CreateValidator(cache);

        var result = await validator.ValidateAsync(UserId, "any-token");

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task ValidateAsync_WhenPayloadUserIdMismatches_ReturnsFalse()
    {
        // 키는 session_42인데 본문 UserId가 99인 손상/위조 페이로드는 거부한다.
        var cache = new Mock<IDistributedCache>();
        SetupCache(cache, $"session_{UserId}", SerializeWebSession(99, "good-token"));
        var validator = CreateValidator(cache);

        var result = await validator.ValidateAsync(UserId, "good-token");

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task ValidateAsync_WithEmptyToken_ReturnsFalseWithoutCacheLookup()
    {
        var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        var validator = CreateValidator(cache);

        var result = await validator.ValidateAsync(UserId, string.Empty);

        Assert.That(result.IsValid, Is.False);
        cache.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ValidateAsync_LegacySessionWithoutNickname_IsValidWithNullNickname()
    {
        // 닉네임 연동 이전에 발급된 세션(Nickname 필드 없음)도 토큰만 맞으면 통과하고, 닉네임은 null이다.
        var legacyJson = $"{{\"UserId\":{UserId},\"Token\":\"good-token\"}}";
        var cache = new Mock<IDistributedCache>();
        SetupCache(cache, $"session_{UserId}", Encoding.UTF8.GetBytes(legacyJson));
        var validator = CreateValidator(cache);

        var result = await validator.ValidateAsync(UserId, "good-token");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Nickname, Is.Null);
    }
}
