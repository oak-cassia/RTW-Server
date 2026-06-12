using NetworkDefinition.ErrorCode;
using RTWWebServer.Exceptions;

namespace RTWWebServer.Extensions;

public static class HttpContextExtensions
{
    public const string UserIdItemKey = "UserId";

    public static bool TryGetAuthenticatedUserId(this HttpContext context, out long userId)
    {
        if (context.Items.TryGetValue(UserIdItemKey, out var value) && value is long id && id > 0)
        {
            userId = id;
            return true;
        }

        userId = 0;
        return false;
    }

    public static long GetAuthenticatedUserId(this HttpContext context)
    {
        if (!context.TryGetAuthenticatedUserId(out long userId))
        {
            throw new GameException("Authenticated user id not found in request context", WebServerErrorCode.InvalidAuthToken);
        }

        return userId;
    }
}
