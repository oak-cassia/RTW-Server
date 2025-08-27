using NetworkDefinition.ErrorCode;
using RTWWebServer.Data;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs;
using RTWWebServer.Exceptions;
using RTWWebServer.MasterDatas.Models;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Providers.MasterData;

namespace RTWWebServer.Services;

public class GameEntryService(
    GameDbContext dbContext,
    IUserRepository userRepository,
    IPlayerCharacterRepository playerCharacterRepository,
    IMasterDataProvider masterDataProvider,
    IUserSessionProvider userSessionProvider,
    ILogger<GameEntryService> logger
) : IGameEntryService
{
    private const int DEFAULT_CHARACTER_ID = 1001;
    public async Task<UserSession> EnterGameAsync(long accountId)
    {
        if (accountId <= 0)
        {
            throw new GameException("Invalid account ID", WebServerErrorCode.InvalidArgument);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var user = await userRepository.GetByAccountIdAsync(accountId);
            if (user == null)
            {
                user = CreateNewUser(accountId);
                await userRepository.CreateAsync(user);
                await dbContext.SaveChangesAsync(); // ID 생성을 위해 저장

                if (user.Id <= 0)
                {
                    throw new GameException("Failed to create user - ID not generated", WebServerErrorCode.DatabaseError);
                }
                
                await CreateDefaultCharacterForNewUserAsync(user.Id);
            }

            await transaction.CommitAsync();

            var userSession = await userSessionProvider.CreateSessionAsync(user.Id);
            return userSession;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private User CreateNewUser(long accountId)
    {
        string nickname = $"User_{accountId}";
        var currentTime = DateTime.UtcNow;
        return new User(
            accountId: accountId,
            nickname: nickname,
            level: 1,
            currentExp: 0,
            currentStamina: 100,
            maxStamina: 100,
            lastStaminaRecharge: currentTime,
            premiumCurrency: 0,
            freeCurrency: 0,
            mainCharacterId: 0,
            createdAt: currentTime,
            updatedAt: currentTime
        );
    }

    private async Task CreateDefaultCharacterForNewUserAsync(long userId)
    {
        // 첫 번째 캐릭터를 기본 캐릭터로 생성
        if (masterDataProvider.TryGetCharacter(DEFAULT_CHARACTER_ID, out CharacterMaster characterMaster) == false)
        {
            logger.LogWarning("No characters available in master data for default character creation");
            return;
        }

        var defaultCharacter = new PlayerCharacter(
            userId: userId,
            characterMasterId: characterMaster.Id,
            level: 1,
            currentExp: 0,
            obtainedAt: DateTime.UtcNow
        );

        await playerCharacterRepository.AddAsync(defaultCharacter);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Created default character {CharacterId} for user {UserId}", defaultCharacter.Id, userId);
    }
}