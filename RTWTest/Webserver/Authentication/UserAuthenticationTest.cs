using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RTWWebServer.Database.Cache;
using RTWWebServer.Middleware;
using NetworkDefinition.ErrorCode;

namespace RTWTest.Webserver.Authentication;

[TestFixture]
[TestOf(typeof(UserAuthentication))]
public class UserAuthenticationTest
{
    private Mock<IRemoteCache> _mockRemoteCache;
    private Mock<ILogger> _mockLogger;
    private Mock<RequestDelegate> _mockNext;

    [SetUp]
    public void Setup()
    {
        _mockRemoteCache = new Mock<IRemoteCache>();
        _mockLogger = new Mock<ILogger>();
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
        var middleware = new UserAuthentication(_mockRemoteCache.Object, _mockLogger.Object, _mockNext.Object);
        var context = CreateHttpContext("/login", "");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        _mockRemoteCache.Verify(cache => cache.UnlockAsync("authToken:valid-token", "valid-token"), Times.Never);
    }

    [Test]
    public async Task ShouldReturnInvalidRequestHttpBody_WhenBodyIsEmpty()
    {
        // Arrange
        var middleware = new UserAuthentication(_mockRemoteCache.Object, _mockLogger.Object, _mockNext.Object);
        var context = CreateHttpContext("/secure", "");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(context.Response.ContentType, Is.EqualTo("application/json"));
        _mockRemoteCache.Verify(cache => cache.UnlockAsync("authToken:valid-token", "valid-token"), Times.Never);
    }

    [Test]
    public async Task ShouldReturnInvalidAuthToken_WhenAuthTokenIsInvalid()
    {
        // Arrange
        var middleware = new UserAuthentication(_mockRemoteCache.Object, _mockLogger.Object, _mockNext.Object);
        var context = CreateHttpContext("/secure", "{\"authToken\":\"invalid-token\"}");

        _mockRemoteCache
            .Setup(cache => cache.GetAsync<string>("authToken:valid-token"))
            .ReturnsAsync((null, WebServerErrorCode.Success));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(context.Response.ContentType, Is.EqualTo("application/json"));
        _mockRemoteCache.Verify(cache => cache.UnlockAsync("authToken:valid-token", "valid-token"), Times.Never);
    }

    [Test]
    public async Task ShouldProceedWithValidAuthToken()
    {
        // Arrange
        var middleware = new UserAuthentication(_mockRemoteCache.Object, _mockLogger.Object, _mockNext.Object);
        var context = CreateHttpContext("/secure", "{\"authToken\":\"valid-token\"}");

        _mockRemoteCache
            .Setup(cache => cache.GetAsync<string>("authToken:valid-token"))
            .ReturnsAsync(("valid-token", WebServerErrorCode.Success));

        _mockRemoteCache
            .Setup(cache => cache.LockAsync("authToken:valid-token", "valid-token"))
            .ReturnsAsync(WebServerErrorCode.Success);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        _mockRemoteCache.Verify(cache => cache.UnlockAsync("authToken:valid-token", "valid-token"), Times.Once);
    }
}