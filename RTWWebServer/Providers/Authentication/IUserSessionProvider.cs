using RTWWebServer.DTOs;

namespace RTWWebServer.Providers.Authentication;

public interface IUserSessionProvider
{
    Task<UserSession> CreateSessionAsync(long userId);
    Task<UserSession?> GetSessionAsync(long userId);
    Task<bool> RemoveSessionAsync(long userId);
    Task<bool> IsValidSessionAsync(long userId, string token);
}