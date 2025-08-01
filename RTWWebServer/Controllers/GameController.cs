using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RTWWebServer.DTOs;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Services;
using NetworkDefinition.ErrorCode;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class GameController(IGameEntryService gameEntryService) : ControllerBase
{
    [HttpPost("enter")]
    [Authorize]
    public async Task<GameResponse<UserSession>> EnterGame()
    {
        var jwtToken = HttpContext.Request.Headers["Authorization"]
            .FirstOrDefault()?.Split(" ").Last();
            
        if (string.IsNullOrEmpty(jwtToken))
        {
            return GameResponse<UserSession>.Fail(WebServerErrorCode.InvalidAuthToken);
        }

        var userSession = await gameEntryService.EnterGameAsync(jwtToken);
        return GameResponse<UserSession>.Ok(userSession);
    }
}