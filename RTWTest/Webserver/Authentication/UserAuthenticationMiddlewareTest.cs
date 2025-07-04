using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.DTOs;
using RTWWebServer.Middlewares;
using RTWWebServer.Providers.Authentication;

namespace RTWTest.Webserver.Authentication;

[TestFixture]
[TestOf(typeof(UserAuthenticationMiddleware))]
public class UserAuthenticationMiddlewareTest
{
    private Mock<IRemoteCache> _mockRemoteCache;
    private IRemoteCacheKeyGenerator _remoteCacheKeyGenerator;
    private Mock<IGuidGenerator> _mockGuiGenerator;
    private Mock<ILogger<UserAuthenticationMiddleware>> _mockLogger;
    private Mock<RequestDelegate> _mockNext;

    [SetUp]
    public void Setup()
    {
        _mockRemoteCache = new Mock<IRemoteCache>();
        _remoteCacheKeyGenerator = new RemoteCacheKeyGenerator();
        _mockGuiGenerator = new Mock<IGuidGenerator>();
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
            _mockRemoteCache.Object,
            _remoteCacheKeyGenerator,
            _mockGuiGenerator.Object,
            _mockLogger.Object,
            _mockNext.Object);
        var context = CreateHttpContext("/Login", "");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        _mockRemoteCache.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ShouldReturnInvalidRequestHttpBody_WhenBodyIsEmpty()
    {
        // Arrange
        var middleware = new UserAuthenticationMiddleware(
            _mockRemoteCache.Object,
            _remoteCacheKeyGenerator,
            _mockGuiGenerator.Object,
            _mockLogger.Object,
            _mockNext.Object);
        var context = CreateHttpContext("/secure", "");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(context.Response.ContentType, Is.EqualTo("application/json"));
        _mockRemoteCache.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ShouldReturnInvalidAuthToken_WhenAuthTokenIsInvalid()
    {
        // Arrange
        var middleware = new UserAuthenticationMiddleware(
            _mockRemoteCache.Object,
            _remoteCacheKeyGenerator,
            _mockGuiGenerator.Object,
            _mockLogger.Object,
            _mockNext.Object);
        var context = CreateHttpContext("/secure", "{\"userId\":1,\"authToken\":\"invalid-token\"}");

        _mockRemoteCache
            .Setup(cache => cache.GetAsync<UserSession>("userSession:1"))
            .ReturnsAsync((null, WebServerErrorCode.Success));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(context.Response.ContentType, Is.EqualTo("application/json"));
        _mockNext.Verify(next => next(context), Times.Never);
    }

    [Test]
    public async Task ShouldProceedWithValidAuthToken()
    {
        // Arrange
        var middleware = new UserAuthenticationMiddleware(
            _mockRemoteCache.Object,
            _remoteCacheKeyGenerator,
            _mockGuiGenerator.Object,
            _mockLogger.Object,
            _mockNext.Object);
        var context = CreateHttpContext("/secure", "{\"userId\":1,\"authToken\":\"valid-token\"}");

        _mockRemoteCache
            .Setup(cache => cache.GetAsync<UserSession>("session_1"))
            .ReturnsAsync((new UserSession(1, "valid-token"), WebServerErrorCode.Success));

        _mockRemoteCache
            .Setup(cache => cache.LockAsync(1, "00000000-0000-0000-0000-000000000000"))
            .ReturnsAsync(WebServerErrorCode.Success);

        _mockGuiGenerator
            .Setup(generator => generator.GenerateGuid())
            .Returns(Guid.Parse("00000000-0000-0000-0000-000000000000"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        _mockRemoteCache.Verify(cache => cache.UnlockAsync(1, "00000000-0000-0000-0000-000000000000"), Times.Once);
    }
}