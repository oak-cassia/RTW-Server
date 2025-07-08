using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Exceptions;

namespace RTWWebServer.Middlewares;

public class RequestLockingMiddleware(RequestDelegate next, IDistributedCacheAdapter distributedCacheAdapter)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Items.TryGetValue("UserId", out object? userIdObject) || userIdObject is not int userId)
        {
            await next(context);
            return;
        }

        var lockValue = Guid.NewGuid().ToString();
        var lockAcquired = false;
        try
        {
            lockAcquired = await distributedCacheAdapter.LockAsync(userId, lockValue);
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
                await distributedCacheAdapter.UnlockAsync(userId, lockValue);
            }
        }
    }
}