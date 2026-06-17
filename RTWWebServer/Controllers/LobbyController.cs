using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTWWebServer.Authentication;
using RTWWebServer.DTOs;
using RTWWebServer.DTOs.Request;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Extensions;
using RTWWebServer.Services;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(AuthenticationSchemes = SessionAuthenticationDefaults.SchemeName)]
public class LobbyController(ILobbyService lobbyService) : ControllerBase
{
    [HttpGet]
    public async Task<GameResponse<LobbyInfo>> GetLobbyAsync()
    {
        long userId = HttpContext.GetAuthenticatedUserId();

        var lobby = await lobbyService.GetLobbyAsync(userId);
        return GameResponse<LobbyInfo>.Ok(lobby);
    }

    [HttpPost]
    public async Task<GameResponse<LobbyInfo>> SaveLobbyAsync([FromBody] SaveLobbyRequest request)
    {
        long userId = HttpContext.GetAuthenticatedUserId();

        var lobby = await lobbyService.SaveLobbyAsync(userId, request.Items);
        return GameResponse<LobbyInfo>.Ok(lobby);
    }

    [HttpPost("expand")]
    public async Task<GameResponse<LobbyInfo>> ExpandRoomAsync()
    {
        long userId = HttpContext.GetAuthenticatedUserId();

        var lobby = await lobbyService.ExpandRoomAsync(userId);
        return GameResponse<LobbyInfo>.Ok(lobby);
    }
}