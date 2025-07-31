using RTWWebServer.Data.Entities;

namespace RTWWebServer.Services;

public interface IUserService
{
    Task<User> UpdateNicknameAsync(long userId, string nickname);
}