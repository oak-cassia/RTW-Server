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
        // 세션 인증 경로(/Character, /User 등): UserAuthenticationMiddleware가 먼저 실행되어
        // 검증된 userId를 Items에 설정하므로 이를 기준으로 락을 건다.
        if (context.TryGetAuthenticatedUserId(out long userId))
        {
            return keyGenerator.GenerateUserLockKey(userId);
        }

        // JWT 인증 경로(/Game/enter 등): account 기준으로 락을 건다.
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.TryGetSubjectId(out var accountId))
        {
            return keyGenerator.GenerateAccountLockKey(accountId);
        }

        return null; // 인증되지 않은 경로
    }
}