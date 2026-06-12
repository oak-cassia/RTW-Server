using System.Security.Cryptography;
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

        // 재화 차감과 소유 판정은 DB를 기준으로 한다. 캐시를 기준으로 하면 캐시 히트 시
        // 차감 결과가 캐시에 반영되지 않아, 다음 요청이 stale 잔액으로 계산한 값을 DB에 덮어쓴다.
        var user = await userRepository.GetByIdAsync(userId) ?? throw new GameException("User not found", WebServerErrorCode.UserNotFound);

        var ownedCharacterIds = (await playerCharacterRepository.GetByUserIdAsync(userId))
            .Select(pc => pc.CharacterMasterId)
            .ToHashSet();

        var allCharacters = masterDataProvider.GetAllCharacters();
        if (ownedCharacterIds.Count >= allCharacters.Count)
        {
            throw new GameException("No new characters available to obtain", WebServerErrorCode.InvalidRequestHttpBody);
        }

        var selectedCharacterIds = PickRandomIdsWithoutReplacement(allCharacters.Keys, ownedCharacterIds, count);
        var actualCost = selectedCharacterIds.Count * COST_PER_GACHA;

        if (user.PremiumCurrency < actualCost)
        {
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

        user.PremiumCurrency -= actualCost;
        userRepository.Update(user);

        await dbContext.SaveChangesAsync();

        await InvalidatePlayerCharactersCacheAsync(userId);

        return new CharacterGachaResult
        {
            CharacterMasterIds = selectedCharacterIds,
            RemainingPremiumCurrency = user.PremiumCurrency,
            RemainingFreeCurrency = user.FreeCurrency
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