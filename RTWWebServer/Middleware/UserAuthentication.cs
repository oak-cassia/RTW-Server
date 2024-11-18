using System.Text.Json;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Database.Cache;
using RTWWebServer.DTO.response;

namespace RTWWebServer.Middleware;

public class UserAuthentication(IRemoteCache remoteCache, ILogger logger, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        var path = context.Request.Path.Value ?? string.Empty;
        if (IsExcludedPath(path))
        {
            await next(context);
            return;
        }

        var requestBody = await ReadRequestBodyAsync(context);
        if (string.IsNullOrEmpty(requestBody))
        {
            await RespondWithError(context, WebServerErrorCode.InvalidRequestHttpBody);
            return;
        }

        var requestAuthToken = ExtractAuthTokenFromBody(requestBody);
        if (await IsValidAuthToken(requestAuthToken) == false)
        {
            await RespondWithError(context, WebServerErrorCode.InvalidAuthToken);
            return;
        }

        var lockKey = $"authToken:{requestAuthToken}";

        try
        {
            var result = await remoteCache.LockAsync(lockKey, requestAuthToken);
            if (result != WebServerErrorCode.Success)
            {
                await RespondWithError(context, result);
                return;
            }

            await next(context);
        }
        finally
        {
            await remoteCache.UnlockAsync(lockKey, requestAuthToken);
        }
    }

    private bool IsExcludedPath(string path)
    {
        return path.StartsWith("/login", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/account", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        using var bodyReader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await bodyReader.ReadToEndAsync();

        context.Request.Body.Position = 0;

        return body;
    }

    private string ExtractAuthTokenFromBody(string requestBody)
    {
        using var bodyDocument = JsonDocument.Parse(requestBody);
        if (!bodyDocument.RootElement.TryGetProperty("authToken", out var authTokenElement))
        {
            return string.Empty;
        }

        return authTokenElement.GetString() ?? string.Empty;
    }

    private async Task<bool> IsValidAuthToken(string requestAuthToken)
    {
        var key = $"authToken:{requestAuthToken}";
        var (cachedAuthToken, errorCode) = await remoteCache.GetAsync<string>(key);

        if (errorCode != WebServerErrorCode.Success)
        {
            return false;
        }

        return cachedAuthToken == requestAuthToken;
    }

    private async Task RespondWithError(HttpContext context, WebServerErrorCode errorCode)
    {
        context.Response.ContentType = "application/json";
        
        var errorJson = JsonSerializer.Serialize(new UserAuthenticationResponse(errorCode));
        await context.Response.WriteAsync(errorJson);
    }
}