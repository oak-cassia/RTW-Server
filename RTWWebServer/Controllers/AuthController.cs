using Microsoft.AspNetCore.Mvc;
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
}