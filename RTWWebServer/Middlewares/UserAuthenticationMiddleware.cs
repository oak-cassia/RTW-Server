using NetworkDefinition.ErrorCode;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Exceptions;
using RTWWebServer.Extensions;

namespace RTWWebServer.Middlewares;

public class UserAuthenticationMiddleware(
    IUserSessionProvider userSessionProvider,
    RequestDelegate next)
{
    public const string UserIdHeader = "X-User-Id";
    public const string AuthTokenHeader = "X-Auth-Token";

    private static readonly string[] JWT_AUTHENTICATED_PATHS =
    [
        "/Game/enter"
    ];

    private static readonly string[] EXCLUDED_PATHS =
    [
        "/Login",
        "/Account"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        string path = context.Request.Path.Value ?? string.Empty;

        // 인증이 필요 없는 경로나 JWT로 인증되는 경로는 건너뛰기
        if (MatchesAny(EXCLUDED_PATHS, path) || MatchesAny(JWT_AUTHENTICATED_PATHS, path))
        {
            await next(context);
            return;
        }

        // 세션 기반 인증: X-User-Id / X-Auth-Token 헤더 사용
        if (!long.TryParse(context.Request.Headers[UserIdHeader], out long userId) || userId <= 0)
        {
            throw new GameException($"Missing or invalid {UserIdHeader} header", WebServerErrorCode.InvalidAuthToken);
        }

        string? authToken = context.Request.Headers[AuthTokenHeader];
        if (string.IsNullOrEmpty(authToken))
        {
            throw new GameException($"Missing {AuthTokenHeader} header", WebServerErrorCode.InvalidAuthToken);
        }

        if (!await userSessionProvider.IsValidSessionAsync(userId, authToken))
        {
            throw new GameException($"Invalid or expired auth token for userId: {userId}", WebServerErrorCode.InvalidAuthToken);
        }

        context.Items[HttpContextExtensions.UserIdItemKey] = userId;

        await next(context);
    }

    private static bool MatchesAny(string[] prefixes, string path)
    {
        return prefixes.Any(prefix => IsPathSegmentPrefix(path, prefix));
    }

    // "/Account"가 "/Accountxyz"에는 매칭되지 않도록 세그먼트 경계까지 확인
    private static bool IsPathSegmentPrefix(string path, string prefix)
    {
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path.Length == prefix.Length || path[prefix.Length] == '/';
    }
}
