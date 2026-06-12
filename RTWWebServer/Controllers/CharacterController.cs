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
public class CharacterController(ICharacterGachaService characterGachaService) : ControllerBase
{
    [HttpPost("gacha")]
    public async Task<GameResponse<CharacterGachaResult>> PerformGachaAsync([FromBody] CharacterGachaRequest request)
    {
        long userId = HttpContext.GetAuthenticatedUserId();

        var result = await characterGachaService.PerformGachaAsync(userId, request.GachaType, request.Count);
        return GameResponse<CharacterGachaResult>.Ok(result);
    }

    [HttpGet("owned")]
    public async Task<GameResponse<PlayerCharacterInfo[]>> GetOwnedCharactersAsync()
    {
        long userId = HttpContext.GetAuthenticatedUserId();

        var characters = await characterGachaService.GetPlayerCharactersAsync(userId);
        return GameResponse<PlayerCharacterInfo[]>.Ok(characters);
    }
}
