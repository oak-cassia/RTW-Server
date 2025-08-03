using NetworkDefinition.ErrorCode;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs;
using RTWWebServer.Enums;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.Authentication;

namespace RTWWebServer.Services;

public class GameEntryService(
    IGameUnitOfWork unitOfWork,
    IJwtTokenProvider jwtTokenProvider,
    IUserSessionProvider userSessionProvider,
    ILogger<GameEntryService> logger
) : IGameEntryService
{
    public async Task<UserSession> EnterGameAsync(string jwtToken)
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
        {
            throw new GameException("JWT token is required", WebServerErrorCode.InvalidAuthToken);
        }

        var tokenInfo = jwtTokenProvider.ParseJwtToken(jwtToken);
        if (tokenInfo?.IsValid != true)
        {
            throw new GameException("Invalid JWT token", WebServerErrorCode.InvalidAuthToken);
        }

        User user = await GetOrCreateUserFromTokenInfoAsync(tokenInfo);

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

        var userSession = await userSessionProvider.CreateSessionAsync(user.Id, jwtToken);

        return userSession;
    }

    private async Task<User> GetOrCreateUserFromTokenInfoAsync(JwtTokenInfo tokenInfo)
    {
        if (!tokenInfo.UserRole.HasValue)
        {
            throw new GameException("User role is missing from JWT token", WebServerErrorCode.InvalidAuthToken);
        }

        switch (tokenInfo.UserRole.Value)
        {
            case UserRole.Normal:
            {
                if (string.IsNullOrEmpty(tokenInfo.Email))
                {
                    throw new GameException("Failed to extract email from JWT token", WebServerErrorCode.InvalidAuthToken);
                }

                // Account ID를 통해 User를 조회하도록 변경
                return await GetOrCreateUserByAccountIdAsync(tokenInfo.AccountId);
            }

            case UserRole.Guest:
            {
                if (!tokenInfo.Guid.HasValue)
                {
                    throw new GameException("Failed to extract GUID from JWT token", WebServerErrorCode.InvalidAuthToken);
                }

                // Account ID를 통해 Guest User를 조회하도록 변경
                return await GetOrCreateUserByAccountIdAsync(tokenInfo.AccountId);
            }

            default:
                throw new GameException("Unsupported user role", WebServerErrorCode.InvalidAuthToken);
        }
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