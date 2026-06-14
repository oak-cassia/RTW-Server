using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NetworkDefinition.ErrorCode;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Extensions;
using RTWWebServer.Providers.Authentication;

namespace RTWWebServer.Authentication;

public class SessionAuthenticationHandler(
    IOptionsMonitor<SessionAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IUserSessionProvider userSessionProvider)
    : AuthenticationHandler<SessionAuthenticationOptions>(options, loggerFactory, encoder)
{
    protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? userIdHeader = Request.Headers[SessionAuthenticationDefaults.UserIdHeaderName];
        string? authToken = Request.Headers[SessionAuthenticationDefaults.AuthTokenHeaderName];

        // 세션 헤더가 전혀 없으면 이 스킴으로는 판단하지 않는다 (다른 스킴이 처리할 수 있도록)
        if (userIdHeader == null && authToken == null)
        {
            return AuthenticateResult.NoResult();
        }

        if (!long.TryParse(userIdHeader, NumberStyles.Integer, CultureInfo.InvariantCulture, out long userId) || userId <= 0)
        {
            return AuthenticateResult.Fail($"Missing or invalid {SessionAuthenticationDefaults.UserIdHeaderName} header");
        }

        if (string.IsNullOrEmpty(authToken))
        {
            return AuthenticateResult.Fail($"Missing {SessionAuthenticationDefaults.AuthTokenHeaderName} header");
        }

        if (!await userSessionProvider.IsValidSessionAsync(userId, authToken))
        {
            return AuthenticateResult.Fail($"Invalid or expired session token for userId: {userId}");
        }

        // 컨트롤러와 락 미들웨어가 검증된 userId를 단일 출처에서 읽도록 Items에 노출
        Context.Items[HttpContextExtensions.UserIdItemKey] = userId;

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture))],
            Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // 인증 실패 응답도 GameResponse 형태를 유지해 클라이언트가 에러 코드를 읽을 수 있게 한다
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        await Response.WriteAsJsonAsync(GameResponse.Fail(WebServerErrorCode.InvalidAuthToken));
    }
}
