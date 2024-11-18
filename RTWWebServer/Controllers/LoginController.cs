using Microsoft.AspNetCore.Mvc;
using NetworkDefinition.ErrorCode;
using RTWWebServer.DTO.Request;
using RTWWebServer.DTO.response;
using RTWWebServer.Service;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController(ILogger<LoginController> logger, ILoginService loginService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<LoginResponse> Login([FromBody] LoginRequest request)
    {
        try
        {
            var (errorCode, authToken) = await loginService.LoginAsync(request.Email, request.Password);
            if (errorCode != WebServerErrorCode.Success)
            {
                return new LoginResponse(errorCode, string.Empty);
            }

            return new LoginResponse(WebServerErrorCode.Success, authToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to login");
            return new LoginResponse(WebServerErrorCode.InternalServerError, string.Empty);
        }
    }

    [HttpPost("guestLogin")]
    public async Task<GuestLoginResponse> GuestLogin([FromBody] GuestLoginRequest request)
    {
        try
        {
            var (errorCode, authToken) = await loginService.GuestLoginAsync(request.GuestGuid);
            if (errorCode != WebServerErrorCode.Success)
            {
                return new GuestLoginResponse(errorCode, string.Empty);
            }

            return new GuestLoginResponse(WebServerErrorCode.Success, authToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to login as guest");
            return new GuestLoginResponse(WebServerErrorCode.InternalServerError, string.Empty);
        }
    }
}