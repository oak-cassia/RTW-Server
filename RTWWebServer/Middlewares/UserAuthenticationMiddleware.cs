using NetworkDefinition.ErrorCode;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Exceptions;
using System.Text.Json;

namespace RTWWebServer.Middlewares;

public class UserAuthenticationMiddleware(
    IUserSessionProvider userSessionProvider,
    ILogger<UserAuthenticationMiddleware> logger,
    RequestDelegate next)
{
    private static readonly HashSet<string> JWT_AUTHENTICATED_PATHS =
    [
        "/Game/enter"
    ];

    private static readonly HashSet<string> EXCLUDED_PATHS =
    [
        "/Login",
        "/Account"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        string path = context.Request.Path.Value ?? string.Empty;
        
        // 인증이 필요 없는 경로나 JWT로 인증되는 경로는 건너뛰기
        if (IsExcludedPath(path) || IsJwtAuthenticatedPath(path))
        {
            await next(context);
            return;
        }

        // 세션 기반 인증
        string requestBody = await ReadRequestBodyAsync(context);
        if (string.IsNullOrEmpty(requestBody))
            throw new GameException("Failed to read request body", WebServerErrorCode.InvalidRequestHttpBody);

        (long userId, string authToken) = ExtractUserIdAndAuthToken(requestBody);
        if (userId <= 0 || string.IsNullOrEmpty(authToken))
            throw new GameException("Failed to extract user id and auth token from request body", WebServerErrorCode.InvalidRequestHttpBody);

        if (!await userSessionProvider.IsValidSessionAsync(userId, authToken))
            throw new GameException($"Invalid or expired auth token for userId: {userId}", WebServerErrorCode.InvalidAuthToken);

        context.Items["UserId"] = userId;

        await next(context);
    }

    private bool IsExcludedPath(string path) => EXCLUDED_PATHS.Any(path.StartsWith);

    private bool IsJwtAuthenticatedPath(string path) => JWT_AUTHENTICATED_PATHS.Any(path.StartsWith);

    private async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        try
        {
            using var bodyReader = new StreamReader(context.Request.Body, leaveOpen: true);
            string body = await bodyReader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            return body;
        }
        catch
        {
            return string.Empty;
        }
    }

    private (long, string) ExtractUserIdAndAuthToken(string requestBody)
    {
        try
        {
            using var bodyDocument = JsonDocument.Parse(requestBody);
            var root = bodyDocument.RootElement;

            if (!root.TryGetProperty("authToken", out var authTokenEl) ||
                !root.TryGetProperty("userId", out var userIdEl))
            {
                return (0, string.Empty);
            }

            // JSON이 숫자면 GetInt64, 문자열이면 TryParse
            long userId = 0;

            switch (userIdEl.ValueKind)
            {
                case JsonValueKind.String:
                    long.TryParse(userIdEl.GetString(), out userId);
                    break;

                case JsonValueKind.Number:
                    userId = userIdEl.GetInt64();
                    break;
            }

            string authToken = authTokenEl.GetString() ?? string.Empty;
            return (userId, authToken);
        }
        catch
        {
            return (0, string.Empty);
        }
    }
}