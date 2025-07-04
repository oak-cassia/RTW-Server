using RTWWebServer.Entities;

namespace RTWWebServer.Services;

public interface IUserSessionManager
{
    Task<UserSession> CreateSessionAsync(int userId, string token);
    Task<UserSession?> GetSessionAsync(string token);
    Task<bool> RemoveSessionAsync(string token);
    Task<bool> IsValidSessionAsync(string token);
}