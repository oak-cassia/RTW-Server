using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RTWWebServer.Authentication;
using RTWWebServer.Extensions;
using RTWWebServer.Providers.Authentication;

namespace RTWTest.WebServer.Authentication;

[TestFixture]
[TestOf(typeof(SessionAuthenticationHandler))]
public class SessionAuthenticationHandlerTest
{
    private Mock<IUserSessionProvider> _mockUserSessionProvider;

    [SetUp]
    public void Setup()
    {
        _mockUserSessionProvider = new Mock<IUserSessionProvider>();
    }

    private static HttpContext CreateHttpContext(string? userId = null, string? authToken = null)
    {
        var context = new DefaultHttpContext();

        if (userId != null)
        {
            context.Request.Headers[SessionAuthenticationDefaults.UserIdHeaderName] = userId;
        }

        if (authToken != null)
        {
            context.Request.Headers[SessionAuthenticationDefaults.AuthTokenHeaderName] = authToken;
        }

        return context;
    }

    private async Task<SessionAuthenticationHandler> CreateHandlerAsync(HttpContext context)
    {
        var optionsMonitor = new Mock<IOptionsMonitor<SessionAuthenticationOptions>>();
        optionsMonitor
            .Setup(monitor => monitor.Get(SessionAuthenticationDefaults.SchemeName))
            .Returns(new SessionAuthenticationOptions());

        var handler = new SessionAuthenticationHandler(
            optionsMonitor.Object,
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            _mockUserSessionProvider.Object);

        var scheme = new AuthenticationScheme(
            SessionAuthenticationDefaults.SchemeName,
            displayName: null,
            typeof(SessionAuthenticationHandler));

        await handler.InitializeAsync(scheme, context);
        return handler;
    }

    [Test]
    public async Task ShouldReturnNoResult_WhenNoSessionHeaders()
    {
        // Arrange: 세션 헤더가 전혀 없으면 다른 스킴이 처리하도록 NoResult
        var context = CreateHttpContext();
        var handler = await CreateHandlerAsync(context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.That(result.None, Is.True);
        _mockUserSessionProvider.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ShouldFail_WhenUserIdHeaderIsInvalid()
    {
        // Arrange
        var context = CreateHttpContext(userId: "not-a-number", authToken: "some-token");
        var handler = await CreateHandlerAsync(context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure, Is.Not.Null);
        _mockUserSessionProvider.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ShouldFail_WhenAuthTokenHeaderIsMissing()
    {
        // Arrange
        var context = CreateHttpContext(userId: "1");
        var handler = await CreateHandlerAsync(context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure, Is.Not.Null);
        _mockUserSessionProvider.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ShouldFail_WhenSessionIsInvalid()
    {
        // Arrange
        var context = CreateHttpContext(userId: "1", authToken: "invalid-token");
        var handler = await CreateHandlerAsync(context);

        _mockUserSessionProvider
            .Setup(provider => provider.IsValidSessionAsync(1, "invalid-token"))
            .ReturnsAsync(false);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure!.Message, Does.Contain("Invalid or expired session token"));
    }

    [Test]
    public async Task ShouldSucceedAndExposeUserId_WhenSessionIsValid()
    {
        // Arrange
        var context = CreateHttpContext(userId: "1", authToken: "valid-token");
        var handler = await CreateHandlerAsync(context);

        _mockUserSessionProvider
            .Setup(provider => provider.IsValidSessionAsync(1, "valid-token"))
            .ReturnsAsync(true);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier), Is.EqualTo("1"));
        Assert.That(context.Items[HttpContextExtensions.UserIdItemKey], Is.EqualTo(1L));
    }

    [Test]
    public async Task ShouldWriteGameResponse_OnChallenge()
    {
        // Arrange
        var context = CreateHttpContext();
        context.RequestServices = new ServiceCollection().BuildServiceProvider();
        context.Response.Body = new MemoryStream();
        var handler = await CreateHandlerAsync(context);

        // Act
        await handler.ChallengeAsync(properties: null);

        // Assert
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        context.Response.Body.Position = 0;
        string body = Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
        Assert.That(body, Does.Contain("1006")); // WebServerErrorCode.InvalidAuthToken
    }
}
