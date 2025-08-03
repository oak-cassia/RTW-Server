using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;

namespace RTWWebServer.Services;

public class UserService(IGameUnitOfWork unitOfWork)
    : IUserService
{
    public async Task<User> UpdateNicknameAsync(long userId, string nickname)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"invalid user id {userId}");
        }

        var existingUser = await unitOfWork.UserRepository.GetByNicknameAsync(nickname);
        if (existingUser != null)
        {
            throw new KeyNotFoundException($"invalid nickname {nickname}");
        }

        user.Nickname = nickname;
        user.UpdatedAt = DateTime.UtcNow;

        unitOfWork.UserRepository.Update(user);
        await unitOfWork.SaveAsync();
        return user;
    }
}
