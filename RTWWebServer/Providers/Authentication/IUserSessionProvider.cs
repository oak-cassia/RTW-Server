using RTWWebServer.DTOs;

namespace RTWWebServer.Providers.Authentication;

public interface IUserSessionProvider
{
    Task<UserSession> CreateSessionAsync(int userId, string token);
    Task<UserSession?> GetSessionAsync(int userId);
    Task<bool> RemoveSessionAsync(int userId);
    Task<bool> IsValidSessionAsync(int userId, string token);
}