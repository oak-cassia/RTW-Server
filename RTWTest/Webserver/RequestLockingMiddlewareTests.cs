using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Exceptions;
using RTWWebServer.Middlewares;

namespace RTWTest.Webserver;

[TestFixture]
public class RequestLockingMiddlewareTests
{
    private Mock<IDistributedCacheAdapter> _mockDistributedCacheAdapter;
    private Mock<RequestDelegate> _mockNext;
    private Mock<IRemoteCacheKeyGenerator> _mockKeyGenerator;
    private RequestLockingMiddleware _middleware;
    private DefaultHttpContext _httpContext;

    [SetUp]
    public void SetUp()
    {
        _mockDistributedCacheAdapter = new Mock<IDistributedCacheAdapter>();
        _mockNext = new Mock<RequestDelegate>();
        _mockKeyGenerator = new Mock<IRemoteCacheKeyGenerator>();
        _middleware = new RequestLockingMiddleware(_mockNext.Object, _mockDistributedCacheAdapter.Object, _mockKeyGenerator.Object);
        _httpContext = new DefaultHttpContext();
    }

    [Test]
    public async Task InvokeAsync_WhenNotAuthenticated_ShouldCallNextWithoutLocking()
    {
        // Arrange
        // 인증되지 않은 상태 (기본값)

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(next => next(_httpContext), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.LockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockDistributedCacheAdapter.Verify(cache => cache.UnlockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task InvokeAsync_WhenJwtAuthenticated_ShouldLockWithAccountId()
    {
        // Arrange
        const long accountId = 12345;
        const string lockKey = "lock:account:12345";
        
        // JWT 인증된 사용자 설정 - sub 클레임 사용
        var identity = new ClaimsIdentity("jwt");
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()));
        _httpContext.User = new ClaimsPrincipal(identity);
        
        _mockKeyGenerator.Setup(kg => kg.GenerateAccountLockKey(accountId)).Returns(lockKey);
        _mockDistributedCacheAdapter.Setup(cache => cache.LockAsync(lockKey, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(next => next(_httpContext), Times.Once);
        _mockKeyGenerator.Verify(kg => kg.GenerateAccountLockKey(accountId), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.LockAsync(lockKey, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.UnlockAsync(lockKey, It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void InvokeAsync_WhenLockNotAcquired_ShouldThrowGameException()
    {
        // Arrange
        const long accountId = 12345;
        const string lockKey = "lock:account:12345";
        
        // JWT 인증된 사용자 설정 - sub 클레임 사용
        var identity = new ClaimsIdentity("jwt");
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()));
        _httpContext.User = new ClaimsPrincipal(identity);
        
        _mockKeyGenerator.Setup(kg => kg.GenerateAccountLockKey(accountId)).Returns(lockKey);
        _mockDistributedCacheAdapter.Setup(cache => cache.LockAsync(lockKey, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () => await _middleware.InvokeAsync(_httpContext));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.RemoteCacheLockFailed));

        _mockNext.Verify(next => next(_httpContext), Times.Never);
        _mockKeyGenerator.Verify(kg => kg.GenerateAccountLockKey(accountId), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.LockAsync(lockKey, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.UnlockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void InvokeAsync_WhenNextThrowsException_ShouldUnlockAndRethrow()
    {
        // Arrange
        const long accountId = 12345;
        const string lockKey = "lock:account:12345";
        
        // JWT 인증된 사용자 설정 - sub 클레임 사용
        var identity = new ClaimsIdentity("jwt");
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()));
        _httpContext.User = new ClaimsPrincipal(identity);
        
        _mockKeyGenerator.Setup(kg => kg.GenerateAccountLockKey(accountId)).Returns(lockKey);
        _mockDistributedCacheAdapter.Setup(cache => cache.LockAsync(lockKey, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var expectedException = new InvalidOperationException("Test exception");
        _mockNext.Setup(next => next(_httpContext))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(async () => await _middleware.InvokeAsync(_httpContext));

        Assert.That(thrownException, Is.SameAs(expectedException));
        _mockKeyGenerator.Verify(kg => kg.GenerateAccountLockKey(accountId), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.LockAsync(lockKey, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.UnlockAsync(lockKey, It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenUsingSubClaim_ShouldWork()
    {
        // Arrange
        const long accountId = 12345;
        const string lockKey = "lock:account:12345";
        
        // JWT sub 클레임만 있는 경우
        var identity = new ClaimsIdentity("jwt");
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()));
        _httpContext.User = new ClaimsPrincipal(identity);
        
        _mockKeyGenerator.Setup(kg => kg.GenerateAccountLockKey(accountId)).Returns(lockKey);
        _mockDistributedCacheAdapter.Setup(cache => cache.LockAsync(lockKey, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(next => next(_httpContext), Times.Once);
        _mockKeyGenerator.Verify(kg => kg.GenerateAccountLockKey(accountId), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.LockAsync(lockKey, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.UnlockAsync(lockKey, It.IsAny<string>()), Times.Once);
    }
}