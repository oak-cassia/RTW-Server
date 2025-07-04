using Microsoft.AspNetCore.Mvc;
using NetworkDefinition.ErrorCode;
using RTWWebServer.DTOs.Request;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Services;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController(ILoginService loginService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<LoginResponse> Login([FromBody] LoginRequest request)
    {
        string jwt = await loginService.LoginAsync(request.Email, request.Password);
        return new LoginResponse(WebServerErrorCode.Success, jwt);
    }

    [HttpPost("guestLogin")]
    public async Task<GuestLoginResponse> GuestLogin([FromBody] GuestLoginRequest request)
    {
        string authToken = await loginService.GuestLoginAsync(request.GuestGuid);
        return new GuestLoginResponse(WebServerErrorCode.Success, authToken);
    }
}