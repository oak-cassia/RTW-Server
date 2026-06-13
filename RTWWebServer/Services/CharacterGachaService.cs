using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Data;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.MasterData;

namespace RTWWebServer.Services;

public class CharacterGachaService(
    GameDbContext dbContext,
    IUserRepository userRepository,
    IPlayerCharacterRepository playerCharacterRepository,
    IMasterDataProvider masterDataProvider,
    ICacheManager cacheManager,
    IRemoteCacheKeyGenerator remoteCacheKeyGenerator,
    ILogger<CharacterGachaService> logger
) : ICharacterGachaService
{
    const int COST_PER_GACHA = 300;

    public async Task<CharacterGachaResult> PerformGachaAsync(long userId, int gachaType, int count)
    {
        if (count <= 0)
        {
            throw new GameException("Invalid gacha count", WebServerErrorCode.InvalidRequestHttpBody);
        }

        // 소유 캐릭터를 기준으로 뽑을 대상과 비용을 먼저 정한다 (읽기 전용, 트랜잭션 밖).
        var ownedCharacterIds = (await playerCharacterRepository.GetByUserIdAsync(userId))
            .Select(pc => pc.CharacterMasterId)
            .ToHashSet();

        var allCharacters = masterDataProvider.GetAllCharacters();
        if (ownedCharacterIds.Count >= allCharacters.Count)
        {
            throw new GameException("No new characters available to obtain", WebServerErrorCode.InvalidRequestHttpBody);
        }

        var selectedCharacterIds = PickRandomIdsWithoutReplacement(allCharacters.Keys, ownedCharacterIds, count);
        var actualCost = (long)selectedCharacterIds.Count * COST_PER_GACHA;

        // 재화 차감과 캐릭터 지급을 한 트랜잭션으로 묶는다. 차감은 조건부 UPDATE로 DB가 직접
        // 수행하므로, 분산락이 풀리거나 페일오버해도 잔액 정확성(음수·중복 차감 방지)이 보장된다.
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            if (await userRepository.TryDeductPremiumCurrencyAsync(userId, actualCost) == false)
            {
                await transaction.RollbackAsync();
                throw new GameException("Insufficient premium currency", WebServerErrorCode.InsufficientCurrency);
            }

            foreach (var characterId in selectedCharacterIds)
            {
                var newCharacter = new PlayerCharacter(
                    userId: userId,
                    characterMasterId: characterId,
                    level: 1,
                    currentExp: 0,
                    obtainedAt: DateTime.UtcNow
                );

                await playerCharacterRepository.AddAsync(newCharacter);
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            // 동시 가챠가 같은 캐릭터를 뽑아 uk_user_character를 위반한 경우. 차감을 포함한
            // 트랜잭션 전체가 롤백되므로 재화 손실은 없다. 클라이언트는 재시도하면 된다.
            await transaction.RollbackAsync();
            throw new GameException("Character already obtained, please retry", WebServerErrorCode.DuplicateCharacter);
        }

        await InvalidatePlayerCharactersCacheAsync(userId);

        // ExecuteUpdateAsync는 체인지 트래커를 우회하므로, 응답용 잔액은 커밋 후 새로 읽는다.
        var updatedUser = await userRepository.GetByIdAsNoTrackingAsync(userId)
            ?? throw new GameException("User not found", WebServerErrorCode.UserNotFound);

        return new CharacterGachaResult
        {
            CharacterMasterIds = selectedCharacterIds,
            RemainingPremiumCurrency = updatedUser.PremiumCurrency,
            RemainingFreeCurrency = updatedUser.FreeCurrency
        };
    }

    // 쓰기 후에는 캐시를 갱신(write-through)하지 않고 무효화한다. DB 커밋 후 Redis 쓰기가 실패하면
    // stale 데이터가 TTL까지 남기 때문. 무효화 실패는 조회 캐시에만 영향을 주므로 요청을 실패시키지 않는다.
    private async Task InvalidatePlayerCharactersCacheAsync(long userId)
    {
        try
        {
            await cacheManager.DeleteAsync(remoteCacheKeyGenerator.GeneratePlayerCharactersKey(userId));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate player characters cache for userId: {UserId}", userId);
        }
    }

    public async Task<PlayerCharacterInfo[]> GetPlayerCharactersAsync(long userId)
    {
        var playerCharacters = await GetCachedPlayerCharactersAsync(userId);
        var result = playerCharacters.Select(pc => new PlayerCharacterInfo
        {
            Id = pc.Id,
            CharacterMasterId = pc.CharacterMasterId,
            Level = pc.Level,
            CurrentExp = pc.CurrentExp,
            ObtainedAt = pc.ObtainedAt,
            UpdatedAt = pc.UpdatedAt
        }).ToArray();

        await cacheManager.CommitAllChangesAsync();

        return result;
    }

    private async Task<List<PlayerCharacter>> GetCachedPlayerCharactersAsync(long userId)
    {
        var cacheKey = remoteCacheKeyGenerator.GeneratePlayerCharactersKey(userId);
        var cachedCharacters = await cacheManager.GetAsync<List<PlayerCharacter>>(cacheKey);
        if (cachedCharacters is not null)
        {
            return cachedCharacters;
        }

        var characters = (await playerCharacterRepository.GetByUserIdAsync(userId)).ToList();
        cacheManager.Set(cacheKey, characters);

        return characters;
    }

    private static List<int> PickRandomIdsWithoutReplacement(IEnumerable<int> allCharacterIds, HashSet<int> ownedCharacterIds, int requestedCount)
    {
        var unownedCharacterIds = allCharacterIds.Where(id => !ownedCharacterIds.Contains(id)).ToArray();

        var actualCount = Math.Min(requestedCount, unownedCharacterIds.Length);

        ShufflePrefix(unownedCharacterIds.AsSpan(), actualCount);

        var result = new List<int>(actualCount);
        for (var i = 0; i < actualCount; i++)
            result.Add(unownedCharacterIds[i]);

        return result;

    }

    // TODO : 함수 다른 클래스로 분리
    private static void ShufflePrefix(Span<int> values, int prefixCount)
    {
        var length = values.Length;

        prefixCount = Math.Min(length, prefixCount);
        if (prefixCount < 1)
        {
            return;
        }

        for (var i = 0; i < prefixCount; ++i)
        {
            var j = RandomNumberGenerator.GetInt32(i, length);
            if (i != j)
            {
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}