using Microsoft.AspNetCore.Http;
using Moq;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Middlewares;
using RTWWebServer.Exceptions;
using RTWWebServer.Extensions;
using NetworkDefinition.ErrorCode;

namespace RTWTest.WebServer.Authentication;

[TestFixture]
[TestOf(typeof(UserAuthenticationMiddleware))]
public class UserAuthenticationMiddlewareTest
{
    private Mock<IUserSessionProvider> _mockUserSessionProvider;
    private Mock<RequestDelegate> _mockNext;
    private UserAuthenticationMiddleware _middleware;

    [SetUp]
    public void Setup()
    {
        _mockUserSessionProvider = new Mock<IUserSessionProvider>();
        _mockNext = new Mock<RequestDelegate>();
        _middleware = new UserAuthenticationMiddleware(_mockUserSessionProvider.Object, _mockNext.Object);
    }

    private static HttpContext CreateHttpContext(string path, string? userId = null, string? authToken = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        if (userId != null)
        {
            context.Request.Headers[UserAuthenticationMiddleware.UserIdHeader] = userId;
        }

        if (authToken != null)
        {
            context.Request.Headers[UserAuthenticationMiddleware.AuthTokenHeader] = authToken;
        }

        return context;
    }

    [Test]
    public async Task ShouldSkipExcludedPath()
    {
        // Arrange
        var context = CreateHttpContext("/Login/login");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        _mockUserSessionProvider.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ShouldSkipJwtAuthenticatedPath()
    {
        // Arrange
        var context = CreateHttpContext("/Game/enter");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        _mockUserSessionProvider.VerifyNoOtherCalls();
    }

    [Test]
    public void ShouldNotSkipPathWithSimilarPrefix()
    {
        // Arrange: "/Account"로 시작하지만 다른 세그먼트인 경로는 제외 대상이 아니다
        var context = CreateHttpContext("/Accountxyz/something");

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () => await _middleware.InvokeAsync(context));
        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidAuthToken));
        _mockNext.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
    }

    [Test]
    public void ShouldThrow_WhenUserIdHeaderIsMissing()
    {
        // Arrange
        var context = CreateHttpContext("/Character/gacha", userId: null, authToken: "some-token");

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () => await _middleware.InvokeAsync(context));
        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidAuthToken));
        _mockUserSessionProvider.VerifyNoOtherCalls();
        _mockNext.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
    }

    [Test]
    public void ShouldThrow_WhenAuthTokenHeaderIsMissing()
    {
        // Arrange
        var context = CreateHttpContext("/Character/gacha", userId: "1");

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () => await _middleware.InvokeAsync(context));
        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidAuthToken));
        _mockUserSessionProvider.VerifyNoOtherCalls();
        _mockNext.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
    }

    [Test]
    public void ShouldThrow_WhenAuthTokenIsInvalid()
    {
        // Arrange
        var context = CreateHttpContext("/Character/gacha", userId: "1", authToken: "invalid-token");

        _mockUserSessionProvider
            .Setup(provider => provider.IsValidSessionAsync(1, "invalid-token"))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () => await _middleware.InvokeAsync(context));
        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidAuthToken));
            Assert.That(exception.Message, Does.Contain("Invalid or expired auth token"));
        });
        _mockNext.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
    }

    [Test]
    public async Task ShouldProceedAndSetUserId_WithValidAuthToken()
    {
        // Arrange
        var context = CreateHttpContext("/Character/gacha", userId: "1", authToken: "valid-token");

        _mockUserSessionProvider
            .Setup(provider => provider.IsValidSessionAsync(1, "valid-token"))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.That(context.Items[HttpContextExtensions.UserIdItemKey], Is.EqualTo(1L));
    }
}
