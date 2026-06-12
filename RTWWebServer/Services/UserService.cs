using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Data;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs;
using RTWWebServer.Exceptions;

namespace RTWWebServer.Services;

public class UserService(
    GameDbContext dbContext,
    IUserRepository userRepository)
    : IUserService
{
    private const int MIN_NICKNAME_LENGTH = 2;
    private const int MAX_NICKNAME_LENGTH = 16; // GameDbContext의 Nickname HasMaxLength와 일치해야 함

    public async Task<UserInfo> UpdateNicknameAsync(long userId, string nickname)
    {
        ValidateNickname(nickname);

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new GameException($"User not found: {userId}", WebServerErrorCode.UserNotFound);
        }

        var existingUser = await userRepository.GetByNicknameAsync(nickname);
        if (existingUser != null && existingUser.Id != user.Id)
        {
            throw new GameException($"Nickname already in use: {nickname}", WebServerErrorCode.DuplicateNickname);
        }

        user.Nickname = nickname;
        user.UpdatedAt = DateTime.UtcNow;

        userRepository.Update(user);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            // 사전 중복 검사와 저장 사이에 다른 요청이 같은 닉네임을 선점한 경우 (uk_nickname 위반)
            throw new GameException($"Nickname already in use: {nickname}", WebServerErrorCode.DuplicateNickname);
        }

        return new UserInfo
        {
            Id = user.Id,
            Nickname = user.Nickname,
            Level = user.Level,
            CurrentExp = user.CurrentExp,
            CurrentStamina = user.CurrentStamina,
            MaxStamina = user.MaxStamina,
            PremiumCurrency = user.PremiumCurrency,
            FreeCurrency = user.FreeCurrency,
            MainCharacterId = user.MainCharacterId
        };
    }

    private static void ValidateNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            throw new GameException("Nickname is required", WebServerErrorCode.InvalidRequestHttpBody);
        }

        if (nickname != nickname.Trim())
        {
            throw new GameException("Nickname must not have leading or trailing whitespace", WebServerErrorCode.InvalidRequestHttpBody);
        }

        if (nickname.Length is < MIN_NICKNAME_LENGTH or > MAX_NICKNAME_LENGTH)
        {
            throw new GameException(
                $"Nickname length must be between {MIN_NICKNAME_LENGTH} and {MAX_NICKNAME_LENGTH} characters",
                WebServerErrorCode.InvalidRequestHttpBody);
        }
    }
}
