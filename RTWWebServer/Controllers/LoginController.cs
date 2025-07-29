using Microsoft.AspNetCore.Mvc;
using RTWWebServer.DTOs.Request;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Services;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController(ILoginService loginService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<GameResponse<string>> Login([FromBody] LoginRequest request)
    {
        string jwt = await loginService.LoginAsync(request.Email, request.Password);
        return GameResponse<string>.Ok(jwt);
    }

    [HttpPost("guestLogin")]
    public async Task<GameResponse<string>> GuestLogin([FromBody] GuestLoginRequest request)
    {
        string authToken = await loginService.GuestLoginAsync(request.GuestGuid);
        return GameResponse<string>.Ok(authToken);
    }
}