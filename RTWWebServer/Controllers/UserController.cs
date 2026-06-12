using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTWWebServer.Authentication;
using RTWWebServer.DTOs;
using RTWWebServer.DTOs.Request;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Extensions;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Services;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(AuthenticationSchemes = SessionAuthenticationDefaults.SchemeName)]
public class UserController(IUserService userService, IUserSessionProvider userSessionProvider) : ControllerBase
{
    [HttpPost("nickname")]
    public async Task<GameResponse<UserInfo>> UpdateNicknameAsync([FromBody] UpdateNicknameRequest request)
    {
        long userId = HttpContext.GetAuthenticatedUserId();

        var user = await userService.UpdateNicknameAsync(userId, request.Nickname);
        return GameResponse<UserInfo>.Ok(user);
    }

    [HttpPost("logout")]
    public async Task<GameResponse> LogoutAsync()
    {
        long userId = HttpContext.GetAuthenticatedUserId();

        await userSessionProvider.RemoveSessionAsync(userId);
        return GameResponse.Ok();
    }
}
