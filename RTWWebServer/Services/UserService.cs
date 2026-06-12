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
    public async Task<UserInfo> UpdateNicknameAsync(long userId, string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            throw new GameException("Nickname is required", WebServerErrorCode.InvalidRequestHttpBody);
        }

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
        await dbContext.SaveChangesAsync();

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
}
