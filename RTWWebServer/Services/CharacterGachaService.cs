using System.Security.Cryptography;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.MasterData;

namespace RTWWebServer.Services;

public class CharacterGachaService(IGameUnitOfWork gameUnitOfWork, IMasterDataProvider masterDataProvider) : ICharacterGachaService
{
    const int COST_PER_GACHA = 300;

    public async Task<CharacterGachaResult> PerformGachaAsync(long userId, int gachaType, int count)
    {
        var user = await gameUnitOfWork.UserRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new GameException("User not found", WebServerErrorCode.AccountNotFound);
        }

        // 사용자가 이미 보유한 캐릭터 ID 목록 조회
        var ownedCharacterIds = (await gameUnitOfWork.PlayerCharacterRepository.GetByUserIdAsync(userId))
            .Select(pc => pc.CharacterMasterId)
            .ToHashSet();

        var allCharacters = masterDataProvider.GetAllCharacters();

        // 보유 유닛 < 전체 유닛 검증
        if (ownedCharacterIds.Count >= allCharacters.Count)
        {
            throw new GameException("No new characters available to obtain", WebServerErrorCode.InvalidRequestHttpBody);
        }

        var characterMasterIds = PickDistinct(allCharacters.Keys, ownedCharacterIds, count);

        foreach (var characterId in characterMasterIds)
        {
            // 새 캐릭터 - 보유 목록에 추가
            await gameUnitOfWork.PlayerCharacterRepository.AddAsync(new PlayerCharacter(
                userId: userId,
                characterMasterId: characterId,
                level: 1,
                currentExp: 0,
                obtainedAt: DateTime.UtcNow
            ));
        }

        // 실제 사용한 비용만큼 화폐 차감
        var actualCost = characterMasterIds.Count * COST_PER_GACHA;
        if (user.PremiumCurrency < actualCost)
        {
            throw new GameException("Insufficient premium currency", WebServerErrorCode.InsufficientCurrency);
        }

        user.PremiumCurrency -= actualCost;
        gameUnitOfWork.UserRepository.Update(user);

        await gameUnitOfWork.SaveAsync();

        return new CharacterGachaResult
        {
            CharacterMasterIds = characterMasterIds,
            RemainingPremiumCurrency = user.PremiumCurrency,
            RemainingFreeCurrency = user.FreeCurrency
        };
    }

    public async Task<PlayerCharacterInfo[]> GetPlayerCharactersAsync(long userId)
    {
        var playerCharacters = await gameUnitOfWork.PlayerCharacterRepository.GetByUserIdAsync(userId);

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

    private static List<int> PickDistinct(IEnumerable<int> allCharacterIds, HashSet<int> ownedCharacterIds, int requestedCount)
    {
        // 미보유 캐릭터 ID만 필터링하고 리스트로 변환
        var unownedCharacterIds = allCharacterIds.Where(id => !ownedCharacterIds.Contains(id)).ToArray();

        // 제자리 셔플 (리스트 크기 변경 없음)
        RandomNumberGenerator.Shuffle(unownedCharacterIds.AsSpan());

        // 필요한 개수만큼 리스트 크기 조정
        var actualCount = Math.Min(requestedCount, unownedCharacterIds.Length);

        return new List<int>(unownedCharacterIds[..actualCount]);
    }
}