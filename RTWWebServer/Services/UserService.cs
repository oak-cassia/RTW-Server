using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;

namespace RTWWebServer.Services;

public class UserService(IUserRepository userRepository)
    : IUserService
{
    public async Task<User> UpdateNicknameAsync(long userId, string nickname)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"invalid user id {userId}");
        }

        var existingUser = await userRepository.GetByNicknameAsync(nickname);
        if (existingUser != null)
        {
            throw new KeyNotFoundException($"invalid nickname {nickname}");
        }

        user.Nickname = nickname;

        return await userRepository.UpdateAsync(user);
    }
}