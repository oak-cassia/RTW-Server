using Microsoft.AspNetCore.Mvc;
using NetworkDefinition.ErrorCode;
using RTWWebServer.RequestResponse;
using RTWWebServer.Service;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController(ILogger<LoginController> logger, ILoginService loginService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var userId = await loginService.LoginAsync(request.Email, request.Password);
            return Ok(userId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to login");
            return StatusCode(500);
        }
    }

    [HttpPost("guestLogin")]
    public async Task<IActionResult> GuestLogin([FromBody] GuestLoginRequest request)
    {
        try
        {
            var errorCode = await loginService.GuestLoginAsync(request.GuestGuid);
            return Ok(errorCode);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to login as guest");
            return StatusCode(500);
        }
    }
}