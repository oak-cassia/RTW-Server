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

                return await GetOrCreateUserAsync(
                    () => unitOfWork.UserRepository.GetByEmailAsync(tokenInfo.Email),
                    tokenInfo.Email,
                    null,
                    (int)tokenInfo.UserRole.Value);
            }

            case UserRole.Guest:
            {
                if (!tokenInfo.Guid.HasValue)
                {
                    throw new GameException("Failed to extract GUID from JWT token", WebServerErrorCode.InvalidAuthToken);
                }

                var guidString = tokenInfo.Guid.Value.ToString();
                return await GetOrCreateUserAsync(
                    () => unitOfWork.UserRepository.GetByGuidAsync(guidString),
                    null,
                    guidString,
                    (int)tokenInfo.UserRole.Value);
            }

            default:
                throw new GameException("Unsupported user role", WebServerErrorCode.InvalidAuthToken);
        }
    }

    private async Task<User> GetOrCreateUserAsync(
        Func<Task<User?>> getUserFunc,
        string? email,
        string? guid,
        int userType)
    {
        var user = await getUserFunc();
        if (user != null)
        {
            return user;
        }

        var now = DateTime.UtcNow;
        return new User(
            0, guid, email, userType, null,
            1, 0, 100, 100, now,
            0, 1000, null, now, now);
    }
}