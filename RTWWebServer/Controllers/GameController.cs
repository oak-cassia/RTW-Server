using Microsoft.AspNetCore.Mvc;
using RTWWebServer.DTOs.Request;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Services;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class GameController(IGameEntryService gameEntryService) : ControllerBase
{
    [HttpPost("enter")]
    public async Task<GameResponse> EnterGame([FromBody] GameEntryRequest request)
    {
        var sessionKey = await gameEntryService.EnterGameAsync(request.JwtToken);
        return GameResponse.Ok();
    }
}