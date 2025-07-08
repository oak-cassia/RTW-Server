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
    private RequestLockingMiddleware _middleware;
    private DefaultHttpContext _httpContext;

    [SetUp]
    public void SetUp()
    {
        _mockDistributedCacheAdapter = new Mock<IDistributedCacheAdapter>();
        _mockNext = new Mock<RequestDelegate>();
        _middleware = new RequestLockingMiddleware(_mockNext.Object, _mockDistributedCacheAdapter.Object);
        _httpContext = new DefaultHttpContext();
    }

    [Test]
    public async Task InvokeAsync_WhenUserIdNotInContext_ShouldCallNextWithoutLocking()
    {
        // Arrange
        // UserId가 context에 없는 상태

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(next => next(_httpContext), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.LockAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockDistributedCacheAdapter.Verify(cache => cache.UnlockAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task InvokeAsync_WhenLockAcquiredSuccessfully_ShouldCallNextAndUnlock()
    {
        // Arrange
        const int userId = 123;
        _httpContext.Items["UserId"] = userId;
        _mockDistributedCacheAdapter.Setup(cache => cache.LockAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(next => next(_httpContext), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.LockAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.UnlockAsync(userId, It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void InvokeAsync_WhenLockNotAcquired_ShouldThrowGameException()
    {
        // Arrange
        const int userId = 123;
        _httpContext.Items["UserId"] = userId;
        _mockDistributedCacheAdapter.Setup(cache => cache.LockAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // 락 실패 시 false 반환

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () => await _middleware.InvokeAsync(_httpContext));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.RemoteCacheLockFailed));

        _mockNext.Verify(next => next(_httpContext), Times.Never);
        _mockDistributedCacheAdapter.Verify(cache => cache.LockAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.UnlockAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void InvokeAsync_WhenNextThrowsException_ShouldUnlockAndRethrow()
    {
        // Arrange
        const int userId = 123;
        _httpContext.Items["UserId"] = userId;
        _mockDistributedCacheAdapter.Setup(cache => cache.LockAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var expectedException = new InvalidOperationException("Test exception");
        _mockNext.Setup(next => next(_httpContext))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(async () => await _middleware.InvokeAsync(_httpContext));

        Assert.That(thrownException, Is.SameAs(expectedException));
        _mockDistributedCacheAdapter.Verify(cache => cache.LockAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCacheAdapter.Verify(cache => cache.UnlockAsync(userId, It.IsAny<string>()), Times.Once);
    }
}