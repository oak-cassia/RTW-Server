using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Exceptions;
using RTWWebServer.Extensions;

namespace RTWWebServer.Middlewares;

public class RequestLockingMiddleware(
    RequestDelegate next,
    IDistributedCacheAdapter distributedCacheAdapter,
    IRemoteCacheKeyGenerator keyGenerator,
    ILogger<RequestLockingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // 상태를 변경하지 않는 읽기 요청은 직렬화할 필요가 없으므로 Redis 락 비용을 생략한다
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

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
                try
                {
                    await distributedCacheAdapter.UnlockAsync(lockKey, lockValue);
                }
                catch (Exception ex)
                {
                    // 응답 본문이 이미 시작된 뒤 언락이 던지면 깨진 응답이 된다. 락은 TTL로 자동 해제되므로
                    // 언락 실패는 경고만 남기고 요청을 끝까지 정상 완료시킨다.
                    logger.LogWarning(ex, "Failed to release distributed lock for key: {LockKey}", lockKey);
                }
            }
        }
    }

    private string? ResolveLockKey(HttpContext context)
    {
        // 세션 인증 경로(/Character, /User 등): SessionAuthenticationHandler가 인증 시
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