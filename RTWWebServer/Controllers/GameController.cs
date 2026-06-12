using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using NetworkDefinition.ErrorCode;
using RTWWebServer.DTOs;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Exceptions;
using RTWWebServer.Services;
using RTWWebServer.Extensions;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class GameController(IGameEntryService gameEntryService) : ControllerBase
{
    [HttpPost("enter")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<GameResponse<UserSession>> EnterGame()
    {
        if (!User.TryGetSubjectId(out long accountId))
        {
            throw new GameException("Subject claim not found in JWT", WebServerErrorCode.InvalidAuthToken);
        }

        var userSession = await gameEntryService.EnterGameAsync(accountId);
        return GameResponse<UserSession>.Ok(userSession);
    }
}
