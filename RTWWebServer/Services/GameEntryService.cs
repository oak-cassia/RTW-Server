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

        if (!jwtTokenProvider.ValidateJwt(jwtToken))
        {
            throw new GameException("Invalid JWT token", WebServerErrorCode.InvalidAuthToken);
        }

        // JWT에서 role 추출
        UserRole? userRole = jwtTokenProvider.GetUserRoleFromJwt(jwtToken);
        if (!userRole.HasValue)
        {
            throw new GameException("Failed to extract role from JWT token", WebServerErrorCode.InvalidAuthToken);
        }

        User? user = null;
        if (userRole == UserRole.Normal)
        {
            string? email = jwtTokenProvider.GetEmailFromJwt(jwtToken);
            if (email == null)
            {
                throw new GameException("Failed to extract email from JWT token", WebServerErrorCode.InvalidAuthToken);
            }

            user = await GetOrCreateUserByEmailAsync(email, (int)userRole);
        }
        else if (userRole == UserRole.Guest)
        {
            var guidFromJwt = jwtTokenProvider.GetGuidFromJwt(jwtToken);
            if (guidFromJwt == null)
            {
                throw new GameException("Failed to extract GUID from JWT token", WebServerErrorCode.InvalidAuthToken);
            }

            var guidString = guidFromJwt.ToString();
            if (guidString == null)
            {
                throw new GameException("Failed to extract GUID from JWT token", WebServerErrorCode.InvalidAuthToken);
            }

            user = await GetOrCreateUserByGuidAsync(guidString, (int)userRole);
        }

        if (user == null)
        {
            throw new GameException("Failed to extract user", WebServerErrorCode.InvalidAuthToken);
        }

        var userSession = await userSessionProvider.CreateSessionAsync(user.Id, jwtToken);

        return userSession;
    }

    private async Task<User> GetOrCreateUserByEmailAsync(string email, int userType)
    {
        var user = await unitOfWork.UserRepository.GetByEmailAsync(email);
        if (user != null)
        {
            return user;
        }

        // TODO
        var now = DateTime.UtcNow;
        return await unitOfWork.UserRepository.CreateAsync(new User(
            0, null, email, userType, null,
            1, 0, 100, 100, now,
            0, 1000, null, now, now));
    }

    private async Task<User> GetOrCreateUserByGuidAsync(string guid, int userType)
    {
        var user = await unitOfWork.UserRepository.GetByGuidAsync(guid);
        if (user != null)
        {
            return user;
        }

        // TODO
        var now = DateTime.UtcNow;
        return await unitOfWork.UserRepository.CreateAsync(new User(
            0, guid, null, userType, null,
            1, 0, 100, 100, now,
            0, 1000, null, now, now));
    }
}