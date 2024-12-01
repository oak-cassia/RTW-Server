using System.Text.Json;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Authentication;
using RTWWebServer.Database.Cache;
using RTWWebServer.Database.Entity;
using RTWWebServer.DTO.response;

namespace RTWWebServer.Middleware;

public class UserAuthenticationMiddleware(
    IRemoteCache remoteCache,
    IRemoteCacheKeyGenerator remoteCacheKeyGenerator,
    IGuidGenerator guidGenerator,
    ILogger<UserAuthenticationMiddleware> logger,
    RequestDelegate next)
{
    const string RESPONSE_CONTENT_TYPE = "application/json";

    private static readonly HashSet<string> EXCLUDED_PATHS =
    [
        "/Login",
        "/Account"
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

        await HandleRequest(context, userId, authToken, next);
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

    private (int userId, string authToken) ExtractUserIdAndAuthToken(string requestBody)
    {
        try
        {
            using JsonDocument bodyDocument = JsonDocument.Parse(requestBody);

            if (!bodyDocument.RootElement.TryGetProperty("userId", out JsonElement userIdElement) ||
                !bodyDocument.RootElement.TryGetProperty("authToken", out JsonElement authTokenElement))
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

    private async Task HandleRequest(HttpContext context, int userId, string authToken, RequestDelegate nextMiddleware)
    {
        string lockValue = guidGenerator.GenerateGuid().ToString();

        try
        {
            WebServerErrorCode result = await remoteCache.LockAsync(userId, lockValue);
            if (result != WebServerErrorCode.Success)
            {
                logger.LogError($"Failed to lock user {userId}");
                await RespondWithError(context, result);

                return;
            }

            if (!await IsValidUserAuthToken(userId, authToken))
            {
                logger.LogError($"Invalid auth token for user {userId}");
                await RespondWithError(context, WebServerErrorCode.InvalidAuthToken);
                return;
            }

            await nextMiddleware(context);
        }
        finally
        {
            await remoteCache.UnlockAsync(userId, lockValue);
        }
    }

    private async Task<bool> IsValidUserAuthToken(int userId, string requestAuthToken)
    {
        string key = remoteCacheKeyGenerator.GenerateUserSessionKey(userId);
        (UserSession? cachedAuthToken, WebServerErrorCode errorCode) = await remoteCache.GetAsync<UserSession>(key);

        if (errorCode != WebServerErrorCode.Success)
        {
            return false;
        }

        return cachedAuthToken?.AuthToken == requestAuthToken;
    }

    private async Task RespondWithError(HttpContext context, WebServerErrorCode errorCode)
    {
        context.Response.ContentType = RESPONSE_CONTENT_TYPE;

        string errorJson = JsonSerializer.Serialize(new UserAuthenticationResponse(errorCode));
        await context.Response.WriteAsync(errorJson);
    }
}