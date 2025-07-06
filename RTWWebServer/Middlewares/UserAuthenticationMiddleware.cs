using System.Text.Json;
using NetworkDefinition.ErrorCode;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Providers.Authentication;

namespace RTWWebServer.Middlewares;

public class UserAuthenticationMiddleware(
    IUserSessionProvider userSessionProvider,
    ILogger<UserAuthenticationMiddleware> logger,
    RequestDelegate next)
{
    private const string RESPONSE_CONTENT_TYPE = "application/json";

    private static readonly HashSet<string> EXCLUDED_PATHS =
    [
        "/Login",
        "/Account",
        "/Game/enter" // 게임 입장 API는 JWT 토큰으로 별도 인증
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        string path = context.Request.Path.Value ?? string.Empty;
        if (IsExcludedPath(path))
        {
            await next(context);
            return;
        }

        string requestBody = await ReadRequestBodyAsync(context);
        if (string.IsNullOrEmpty(requestBody))
        {
            logger.LogError("Failed to read request body");
            await RespondWithError(context, WebServerErrorCode.InvalidRequestHttpBody);
            return;
        }

        (int userId, string authToken) = ExtractUserIdAndAuthToken(requestBody);
        if (userId == default || string.IsNullOrEmpty(authToken))
        {
            logger.LogError("Failed to extract user id and auth token from request body");
            await RespondWithError(context, WebServerErrorCode.InvalidRequestHttpBody);
            return;
        }

        if (!await userSessionProvider.IsValidSessionAsync(userId, authToken))
        {
            logger.LogError($"Invalid or expired auth token for userId: {userId}");
            await RespondWithError(context, WebServerErrorCode.InvalidAuthToken);
            return;
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
                return (default, string.Empty);
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

    private async Task RespondWithError(HttpContext context, WebServerErrorCode errorCode)
    {
        context.Response.ContentType = RESPONSE_CONTENT_TYPE;
        context.Response.StatusCode = 401; // Unauthorized

        string errorJson = JsonSerializer.Serialize(new UserAuthenticationResponse(errorCode));
        await context.Response.WriteAsync(errorJson);
    }
}