using RTWWebServer.DTOs;

namespace RTWWebServer.Services;

public interface IGameEntryService
{
    Task<UserSession> EnterGameAsync(string jwtToken);
}