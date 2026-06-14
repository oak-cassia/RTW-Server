using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Authentication;

/// <summary>
/// 웹 서버가 Redis에 저장한 세션(session_{userId})을 직접 조회해 토큰을 검증한다.
/// 저장 포맷은 웹 서버의 RedisCache(IDistributedCache, Redis hash)에 의존하므로 게임 서버도
/// 동일한 RedisCache 구현으로 읽어야 한다(raw StringGet 불가). 키 규칙·필드명은 웹 서버와
/// 동기화 상태여야 한다 — <see cref="GameUserSession"/> 및 RemoteCacheKeyGenerator 참고.
/// </summary>
public class RedisSessionValidator(IDistributedCache distributedCache, ILogger<RedisSessionValidator> logger) : ISessionValidator
{
    public async Task<bool> ValidateAsync(long userId, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        // 웹 서버 RemoteCacheKeyGenerator.GenerateUserSessionKey와 동일해야 한다
        string sessionKey = $"session_{userId}";

        string? json = await distributedCache.GetStringAsync(sessionKey, cancellationToken);
        if (string.IsNullOrEmpty(json))
        {
            logger.LogDebug("Session not found or expired for userId {UserId}", userId);
            return false;
        }

        GameUserSession? session;
        try
        {
            session = JsonSerializer.Deserialize<GameUserSession>(json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize session payload for userId {UserId}", userId);
            return false;
        }

        if (session is null || session.UserId != userId)
        {
            logger.LogWarning("Session payload mismatch for userId {UserId}", userId);
            return false;
        }

        // 타이밍 공격으로 토큰을 한 글자씩 추측할 수 없도록 상수 시간 비교를 사용한다
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(session.Token),
                Encoding.UTF8.GetBytes(token)))
        {
            logger.LogWarning("Token mismatch for userId {UserId}", userId);
            return false;
        }

        return true;
    }
}
