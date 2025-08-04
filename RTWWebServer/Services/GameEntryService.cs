using NetworkDefinition.ErrorCode;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.Authentication;

namespace RTWWebServer.Services;

public class GameEntryService(
    IGameUnitOfWork unitOfWork,
    IUserSessionProvider userSessionProvider,
    ILogger<GameEntryService> logger
) : IGameEntryService
{
    public async Task<UserSession> EnterGameAsync(long accountId)
    {
        if (accountId <= 0)
        {
            throw new GameException("Invalid account ID", WebServerErrorCode.InvalidAuthToken);
        }

        User user = await GetOrCreateUserByAccountIdAsync(accountId);

        // 새로 생성된 사용자인지 ID로 확인 (ID가 0이면 새로 생성된 사용자)
        if (user.Id == 0)
        {
            await unitOfWork.UserRepository.CreateAsync(user);
            await unitOfWork.SaveAsync();
            // DB 쿼리 후 ID가 설정되었는지 확인
            if (user.Id <= 0)
            {
                throw new GameException("Failed to create user - ID not generated", WebServerErrorCode.DatabaseError);
            }
        }

        var userSession = await userSessionProvider.CreateSessionAsync(user.Id);

        return userSession;
    }


    private async Task<User> GetOrCreateUserByAccountIdAsync(long accountId)
    {
        var user = await unitOfWork.UserRepository.GetByAccountIdAsync(accountId);
        if (user != null)
        {
            return user;
        }

        string nickname = $"User_{accountId}";
        var currentTime = DateTime.UtcNow;
        var newUser = new User(
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

        return await unitOfWork.UserRepository.CreateAsync(newUser);
    }
}