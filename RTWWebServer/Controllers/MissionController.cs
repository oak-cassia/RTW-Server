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
public class MissionController(IMissionService missionService) : ControllerBase
{
    // 예약: 스태미나를 차감하고 티켓을 발급한다. 클라는 이 티켓으로 게임서버에서 전투를 수행한다.
    [HttpPost("start")]
    public async Task<GameResponse<MissionTicketDto>> StartMissionAsync([FromBody] StartMissionRequest request)
    {
        long userId = HttpContext.GetAuthenticatedUserId();

        var ticket = await missionService.StartMissionAsync(userId, request.MissionId, request.CharacterId);
        return GameResponse<MissionTicketDto>.Ok(ticket);
    }

    // 정산: 전투가 끝난 뒤 티켓으로 결과를 제출해 보상을 받는다. 결과 자체는 (게임서버가 기록한)
    // 서버 측 값을 사용하므로 클라가 승패를 위조할 수 없다.
    [HttpPost("end")]
    public async Task<GameResponse<MissionResultDto>> CompleteMissionAsync([FromBody] CompleteMissionRequest request)
    {
        long userId = HttpContext.GetAuthenticatedUserId();

        var result = await missionService.CompleteMissionAsync(userId, request.TicketId);
        return GameResponse<MissionResultDto>.Ok(result);
    }
}
