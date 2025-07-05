using RTWWebServer.DTOs.Request;
using RTWWebServer.DTOs.Response;

namespace RTWWebServer.Services;

public interface IGameEntryService
{
    Task<GameEntryResponse> EnterGameAsync(GameEntryRequest request);
}