using Microsoft.AspNetCore.Mvc;
using RTWWebServer.DTOs.Request;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Extensions;
using RTWWebServer.Services;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class CharacterController(ICharacterGachaService characterGachaService) : ControllerBase
{
    [HttpPost("gacha")]
    public async Task<GameResponse<CharacterGachaResponse>> PerformGachaAsync([FromBody] CharacterGachaRequest request)
    {
        var userId = User.GetUserId();
        var result = await characterGachaService.PerformGachaAsync(userId, request.GachaType, request.Count);
        return GameResponse<CharacterGachaResponse>.Ok(result);
    }

    [HttpGet("owned")]
    public async Task<GameResponse<List<PlayerCharacterInfo>>> GetOwnedCharactersAsync()
    {
        var userId = User.GetUserId();
        var characters = await characterGachaService.GetPlayerCharactersAsync(userId);
        return GameResponse<List<PlayerCharacterInfo>>.Ok(characters);
    }
}