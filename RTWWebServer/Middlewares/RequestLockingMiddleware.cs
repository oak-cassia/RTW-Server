using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Exceptions;

namespace RTWWebServer.Middlewares;

public class RequestLockingMiddleware(RequestDelegate next, IRemoteCache remoteCache)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Items.TryGetValue("UserId", out var userIdObject) || userIdObject is not int userId)
        {
            await next(context);
            return;
        }

        var lockValue = Guid.NewGuid().ToString();
        var lockAcquired = false;
        try
        {
            lockAcquired = await remoteCache.LockAsync(userId, lockValue);
            if (!lockAcquired)
            {
                throw new GameException("Too many requests. Please try again later.", WebServerErrorCode.RemoteCacheLockFailed);
            }

            await next(context);
        }
        finally
        {
            if (lockAcquired)
            {
                await remoteCache.UnlockAsync(userId, lockValue);
            }
        }
    }
}