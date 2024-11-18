using Microsoft.AspNetCore.Mvc;
using NetworkDefinition.ErrorCode;
using RTWWebServer.RequestResponse;
using RTWWebServer.Service;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(ILogger<AuthController> logger, ILoginService loginService) : ControllerBase
{
    private readonly ILogger<AuthController> _logger = logger;
    private readonly ILoginService _loginService = loginService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var userId = await _loginService.LoginAsync(request.Email, request.Password);
            return Ok(userId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to login");
            return StatusCode(500);
        }
    }

    [HttpPost("guestRegister")]
    public async Task<GuestRegisterResponse> GuestRegister()
    {
        try
        {
            var guestGuid = await _loginService.GuestRegisterAsync();

            return new GuestRegisterResponse(WebServerErrorCode.Success, guestGuid);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to register as guest");
            return new GuestRegisterResponse(WebServerErrorCode.InternalServerError, string.Empty);
        }
    }

    [HttpPost("guestLogin")]
    public async Task<IActionResult> GuestLogin([FromBody] GuestLoginRequest request)
    {
        try
        {
            var errorCode = await _loginService.GuestLoginAsync(request.GuestGuid);
            return Ok(errorCode);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to login as guest");
            return StatusCode(500);
        }
    }
}