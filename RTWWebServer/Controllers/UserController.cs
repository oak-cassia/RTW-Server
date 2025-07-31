using Microsoft.AspNetCore.Mvc;
using RTWWebServer.Data.Entities;
using RTWWebServer.DTOs.Request;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Services;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpPost("nickname")]
    public async Task<GameResponse<User>> UpdateNicknameAsync([FromBody] UpdateNicknameRequest request)
    {
        var user = await userService.UpdateNicknameAsync(request.UserId, request.Nickname);
        return GameResponse<User>.Ok(user);
    }
}
