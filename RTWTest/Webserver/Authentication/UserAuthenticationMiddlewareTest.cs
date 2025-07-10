using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Middlewares;
using RTWWebServer.Exceptions;
using NetworkDefinition.ErrorCode;

namespace RTWTest.Webserver.Authentication;

[TestFixture]
[TestOf(typeof(UserAuthenticationMiddleware))]
public class UserAuthenticationMiddlewareTest
{
    private Mock<IUserSessionProvider> _mockUserSessionProvider;
    private Mock<ILogger<UserAuthenticationMiddleware>> _mockLogger;
    private Mock<RequestDelegate> _mockNext;

    [SetUp]
    public void Setup()
    {
        _mockUserSessionProvider = new Mock<IUserSessionProvider>();
        _mockLogger = new Mock<ILogger<UserAuthenticationMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
    }

    private HttpContext CreateHttpContext(string path, string bodyContent)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyContent));
        context.Request.ContentLength = bodyContent.Length;
        context.Request.EnableBuffering();
        return context;
    }

    [Test]
    public async Task ShouldSkipExcludedPath()
    {
        // Arrange
        var middleware = new UserAuthenticationMiddleware(
            _mockUserSessionProvider.Object,
            _mockLogger.Object,
            _mockNext.Object);
        var context = CreateHttpContext("/Login", "");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        _mockUserSessionProvider.VerifyNoOtherCalls();
    }

    [Test]
    public void ShouldReturnInvalidRequestHttpBody_WhenBodyIsEmpty()
    {
        // Arrange
        var middleware = new UserAuthenticationMiddleware(
            _mockUserSessionProvider.Object,
            _mockLogger.Object,
            _mockNext.Object);
        var context = CreateHttpContext("/secure", "");

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () => await middleware.InvokeAsync(context));
        Assert.Multiple(() => 
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidRequestHttpBody));
            Assert.That(exception.Message, Does.Contain("Failed to read request body"));
        });
        _mockUserSessionProvider.VerifyNoOtherCalls();
        _mockNext.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
    }

    [Test]
    public void ShouldReturnInvalidAuthToken_WhenAuthTokenIsInvalid()
    {
        // Arrange
        var middleware = new UserAuthenticationMiddleware(
            _mockUserSessionProvider.Object,
            _mockLogger.Object,
            _mockNext.Object);
        var context = CreateHttpContext("/secure", "{\"userId\":1,\"authToken\":\"invalid-token\"}");

        _mockUserSessionProvider
            .Setup(provider => provider.IsValidSessionAsync(1, "invalid-token"))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () => await middleware.InvokeAsync(context));
        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidAuthToken));
            Assert.That(exception.Message, Does.Contain("Invalid or expired auth token"));
        });
        _mockNext.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
    }

    [Test]
    public async Task ShouldProceedWithValidAuthToken()
    {
        // Arrange
        var middleware = new UserAuthenticationMiddleware(
            _mockUserSessionProvider.Object,
            _mockLogger.Object,
            _mockNext.Object);
        var context = CreateHttpContext("/secure", "{\"userId\":1,\"authToken\":\"valid-token\"}");

        _mockUserSessionProvider
            .Setup(provider => provider.IsValidSessionAsync(1, "valid-token"))
            .ReturnsAsync(true);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
    }
}