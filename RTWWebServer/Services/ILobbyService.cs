using RTWWebServer.DTOs;
using RTWWebServer.DTOs.Request;

namespace RTWWebServer.Services;

public interface ILobbyService
{
    Task<LobbyInfo> GetLobbyAsync(long userId);
    Task<LobbyInfo> SaveLobbyAsync(long userId, IReadOnlyList<LobbyFurniturePlacement> items);
    Task<LobbyInfo> ExpandRoomAsync(long userId);
}
