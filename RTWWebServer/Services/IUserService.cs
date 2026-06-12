using RTWWebServer.DTOs;

namespace RTWWebServer.Services;

public interface IUserService
{
    Task<UserInfo> UpdateNicknameAsync(long userId, string nickname);
}
