using NetworkDefinition.ErrorCode;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers;
using RTWWebServer.Providers.MasterData;

namespace RTWWebServer.Services;

public class CharacterGachaService(IGameUnitOfWork gameUnitOfWork, IMasterDataProvider masterDataProvider) : ICharacterGachaService
{
    const int COST_PER_GACHA = 300;
    private static readonly Random Random = new();

    public async Task<CharacterGachaResponse> PerformGachaAsync(long userId, int gachaType, int count)
    {
        var user = await gameUnitOfWork.UserRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new GameException("User not found", WebServerErrorCode.AccountNotFound);
        }
        
        var totalCost = COST_PER_GACHA * count;

        if (user.PremiumCurrency < totalCost)
        {
            throw new GameException("Insufficient premium currency", WebServerErrorCode.InvalidRequestHttpBody);
        }

        // 사용자가 이미 보유한 캐릭터 목록 조회
        var ownedCharacters = await gameUnitOfWork.PlayerCharacterRepository.GetByUserIdAsync(userId);
        var ownedCharacterIds = ownedCharacters.Select(pc => pc.CharacterMasterId).ToHashSet();
        
        var characterMasterIds = new List<int>();
        var allCharacters = masterDataProvider.GetAllCharacters().ToList();

        if (!allCharacters.Any())
        {
            throw new GameException("No characters available in gacha", WebServerErrorCode.DatabaseError);
        }

        // 미보유 캐릭터만 필터링
        var unownedCharacters = allCharacters.Where(c => !ownedCharacterIds.Contains(c.Id)).ToList();
        
        if (!unownedCharacters.Any())
        {
            throw new GameException("No new characters available to obtain", WebServerErrorCode.InvalidRequestHttpBody);
        }

        // 실제 뽑을 수 있는 횟수는 미보유 캐릭터 수와 요청 횟수 중 작은 값
        var actualGachaCount = Math.Min(count, unownedCharacters.Count);
        
        // 실제 비용 재계산
        var actualCost = COST_PER_GACHA * actualGachaCount;

        for (int i = 0; i < actualGachaCount; i++)
        {
            // 미보유 캐릭터에서 랜덤 선택
            var selectedCharacter = unownedCharacters[Random.Next(unownedCharacters.Count)];
            
            // 새 캐릭터 - 보유 목록에 추가
            var newPlayerCharacter = new PlayerCharacter(
                userId: userId,
                characterMasterId: selectedCharacter.Id,
                level: 1,
                currentExp: 0,
                obtainedAt: DateTime.UtcNow
            );

            await gameUnitOfWork.PlayerCharacterRepository.AddAsync(newPlayerCharacter);
            characterMasterIds.Add(selectedCharacter.Id);
            
            // 선택된 캐릭터를 목록에서 제거하여 중복 선택 방지
            unownedCharacters.Remove(selectedCharacter);
        }

        // 실제 사용한 비용만큼 화폐 차감
        user.PremiumCurrency -= actualCost;
        gameUnitOfWork.UserRepository.Update(user);

        await gameUnitOfWork.SaveAsync();

        return new CharacterGachaResponse
        {
            CharacterMasterIds = characterMasterIds,
            RemainingPremiumCurrency = user.PremiumCurrency,
            RemainingFreeCurrency = user.FreeCurrency
        };
    }

    public async Task<List<PlayerCharacterInfo>> GetPlayerCharactersAsync(long userId)
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
        }).ToList();
    }
}