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
    public async Task<ActionResult<GameEntryResponse>> EnterGame([FromBody] GameEntryRequest request)
    {
        var response = await gameEntryService.EnterGameAsync(request);
        if (response.ErrorCode == NetworkDefinition.ErrorCode.WebServerErrorCode.Success)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}