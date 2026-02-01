using RTWWebServer.DTOs;

namespace RTWWebServer.Services;

public interface IGameEntryService
{
    Task<UserSession> EnterGameAsync(long accountId);
}