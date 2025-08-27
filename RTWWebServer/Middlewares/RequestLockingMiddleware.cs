using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Exceptions;
using RTWWebServer.Extensions;

namespace RTWWebServer.Middlewares;

public class RequestLockingMiddleware(RequestDelegate next, IDistributedCacheAdapter distributedCacheAdapter, IRemoteCacheKeyGenerator keyGenerator)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var lockKey = ResolveLockKey(context);
        if (lockKey == null)
        {
            await next(context);
            return;
        }

        var lockValue = Guid.NewGuid().ToString();
        var lockAcquired = false;

        try
        {
            lockAcquired = await distributedCacheAdapter.LockAsync(lockKey, lockValue);
            if (!lockAcquired)
            {
                throw new GameException($"Failed to acquire lock for key: {lockKey}", WebServerErrorCode.RemoteCacheLockFailed);
            }

            await next(context);
        }
        catch (Exception ex) when (ex is not GameException)
        {
            throw;
        }
        finally
        {
            if (lockAcquired)
            {
                await distributedCacheAdapter.UnlockAsync(lockKey, lockValue);
            }
        }
    }

    private string? ResolveLockKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.TryGetSubjectId(out var accountId))
        {
            return keyGenerator.GenerateAccountLockKey(accountId);
        }

        return null; // 인증되지 않은 경로
    }
}