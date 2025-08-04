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
    // JWT 인증을 사용하는 경로들 (ASP.NET Core의 [Authorize] 어트리뷰트로 처리)
    private static readonly HashSet<string> JWT_AUTHENTICATED_PATHS =
    [
        "/Game/enter"
    ];

    // 인증이 필요 없는 경로들
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

        // UserSession 기반 인증이 필요한 경로들 처리
        string requestBody = await ReadRequestBodyAsync(context);
        if (string.IsNullOrEmpty(requestBody))
        {
            throw new GameException("Failed to read request body", WebServerErrorCode.InvalidRequestHttpBody);
        }

        (int userId, string authToken) = ExtractUserIdAndAuthToken(requestBody);
        if (userId == 0 || string.IsNullOrEmpty(authToken))
        {
            throw new GameException("Failed to extract user id and auth token from request body", WebServerErrorCode.InvalidRequestHttpBody);
        }

        if (!await userSessionProvider.IsValidSessionAsync(userId, authToken))
        {
            throw new GameException($"Invalid or expired auth token for userId: {userId}", WebServerErrorCode.InvalidAuthToken);
        }

        // 세션 정보를 HttpContext에 추가하여 컨트롤러에서 사용할 수 있도록 함
        context.Items["UserId"] = userId;
        context.Items["AuthToken"] = authToken;

        await next(context);
    }

    private bool IsExcludedPath(string path)
    {
        return EXCLUDED_PATHS.Any(path.StartsWith);
    }

    private bool IsJwtAuthenticatedPath(string path)
    {
        return JWT_AUTHENTICATED_PATHS.Any(path.StartsWith);
    }

    private async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        try
        {
            using StreamReader bodyReader = new StreamReader(context.Request.Body, leaveOpen: true);
            string body = await bodyReader.ReadToEndAsync();

            context.Request.Body.Position = 0;

            return body;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private (int, string) ExtractUserIdAndAuthToken(string requestBody)
    {
        try
        {
            using JsonDocument bodyDocument = JsonDocument.Parse(requestBody);

            if (!bodyDocument.RootElement.TryGetProperty("authToken", out JsonElement authTokenElement) ||
                !bodyDocument.RootElement.TryGetProperty("userId", out JsonElement userIdElement))
            {
                return (0, string.Empty);
            }

            int userId = userIdElement.GetInt32();
            string authToken = authTokenElement.GetString() ?? string.Empty;

            return (userId, authToken);
        }
        catch (Exception)
        {
            return (default, string.Empty);
        }
    }
}