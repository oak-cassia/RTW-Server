using Microsoft.EntityFrameworkCore;
using MySqlConnector;
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

        var user = await GetOrCreateUserAsync(accountId);

        // 세션 생성(Redis)은 DB 트랜잭션 밖에서 수행한다. 커밋 이후의 Redis 작업을 같은 try 안에
        // 두면, Redis 장애 시 이미 커밋된 트랜잭션에 RollbackAsync가 호출되어 원래 예외가 가려진다.
        return await userSessionProvider.CreateSessionAsync(user.Id, user.Nickname);
    }

    private async Task<User> GetOrCreateUserAsync(long accountId)
    {
        try
        {
            // 트랜잭션을 try 안에서 열어, 예외 시 catch 진입 전에 await using이 dispose(자동 롤백)하도록 한다.
            // 그래야 catch의 재조회 쿼리가 완료된 트랜잭션에 묶이지 않는다.
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

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

                await CreateDefaultCharacterForNewUserAsync(user);
            }

            await transaction.CommitAsync();
            return user;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            // 락 TTL 만료/failover로 같은 account의 동시 생성이 일어난 경우(uk_account_id) → 재조회해 사용.
            // 기본 닉네임(User_{accountId})의 충돌은 발생하지 않는다: accountId가 유일하고,
            // UserService의 예약 검증(^User_\d+$)이 다른 계정의 선점을 차단하므로 전역 유일성이 보장된다.
            dbContext.ChangeTracker.Clear(); // 실패한 추적 엔티티를 비운 뒤 재조회

            var concurrentlyCreated = await userRepository.GetByAccountIdAsync(accountId);
            return concurrentlyCreated ?? throw new GameException("Failed to create user", WebServerErrorCode.DatabaseError);
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
            mainCharacterId: 0, // 기본 캐릭터 지급 후 그 캐릭터로 채운다(프로필 아바타).
            createdAt: currentTime,
            updatedAt: currentTime
        );
    }

    private async Task CreateDefaultCharacterForNewUserAsync(User user)
    {
        // 첫 번째 캐릭터를 기본 캐릭터로 생성
        if (masterDataProvider.TryGetCharacter(DEFAULT_CHARACTER_ID, out CharacterMaster characterMaster) == false)
        {
            logger.LogWarning("No characters available in master data for default character creation");
            return;
        }

        var defaultCharacter = new PlayerCharacter(
            userId: user.Id,
            characterMasterId: characterMaster.Id,
            level: 1,
            currentExp: 0,
            obtainedAt: DateTime.UtcNow
        );

        await playerCharacterRepository.AddAsync(defaultCharacter);

        // 프로필 대표 캐릭터(아바타)를 방금 지급한 기본 캐릭터로 지정한다. 임무 동작과는 무관한 코스메틱이며,
        // user는 아직 추적 중이라 이 변경은 아래 SaveChanges의 UPDATE로 함께 반영된다.
        user.MainCharacterId = characterMaster.Id;

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Created default character {CharacterId} for user {UserId}", defaultCharacter.Id, user.Id);
    }
}