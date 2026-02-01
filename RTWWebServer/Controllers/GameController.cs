using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RTWWebServer.DTOs;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Services;
using RTWWebServer.Extensions;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class GameController(IGameEntryService gameEntryService) : ControllerBase
{
    [HttpPost("enter")]
    [Authorize]
    public async Task<GameResponse<UserSession>> EnterGame()
    {
        // 컨트롤러에서 인증된 사용자 정보를 추출
        User.TryGetSubjectId(out var accountId);

        var userSession = await gameEntryService.EnterGameAsync(accountId);
        return GameResponse<UserSession>.Ok(userSession);
    }
}