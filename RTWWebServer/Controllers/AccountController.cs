using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTWWebServer.DTOs.Request;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Services;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class AccountController(IAccountService accountService) : ControllerBase
{
    [HttpPost("createGuestAccount")]
    public async Task<GameResponse<string>> CreateGuestAccount()
    {
        string guestGuid = await accountService.CreateGuestAccountAsync();
        return GameResponse<string>.Ok(guestGuid);
    }

    [HttpPost("createAccount")]
    public async Task<GameResponse> CreateAccount([FromBody] CreateAccountRequest request)
    {
        await accountService.CreateAccountAsync(request.Email, request.Password);
        return GameResponse.Ok();
    }
}
