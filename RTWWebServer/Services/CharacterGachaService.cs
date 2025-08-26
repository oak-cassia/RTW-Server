using System.Security.Cryptography;
using NetworkDefinition.ErrorCode;
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
    IMasterDataProvider masterDataProvider) : ICharacterGachaService
{
    const int COST_PER_GACHA = 300;

    public async Task<CharacterGachaResult> PerformGachaAsync(long userId, int gachaType, int count)
    {
        if (count <= 0)
        {
            throw new GameException("Invalid gacha count", WebServerErrorCode.InvalidRequestHttpBody);
        }

        var user = await userRepository.GetByIdAsync(userId) ??
                   throw new GameException("User not found", WebServerErrorCode.AccountNotFound);

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
            await playerCharacterRepository.AddAsync(new PlayerCharacter(
                userId: userId,
                characterMasterId: characterId,
                level: 1,
                currentExp: 0,
                obtainedAt: DateTime.UtcNow
            ));
        }

        user.PremiumCurrency -= actualCost;
        userRepository.Update(user);
        await dbContext.SaveChangesAsync();

        return new CharacterGachaResult
        {
            CharacterMasterIds = selectedCharacterIds,
            RemainingPremiumCurrency = user.PremiumCurrency,
            RemainingFreeCurrency = user.FreeCurrency
        };
    }

    public async Task<PlayerCharacterInfo[]> GetPlayerCharactersAsync(long userId)
    {
        var playerCharacters = await playerCharacterRepository.GetByUserIdAsync(userId);

        return playerCharacters.Select(pc => new PlayerCharacterInfo
        {
            Id = pc.Id,
            CharacterMasterId = pc.CharacterMasterId,
            Level = pc.Level,
            CurrentExp = pc.CurrentExp,
            ObtainedAt = pc.ObtainedAt,
            UpdatedAt = pc.UpdatedAt
        }).ToArray();
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