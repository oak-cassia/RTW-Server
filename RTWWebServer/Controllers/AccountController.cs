using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Services;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController(IAccountService accountService) : ControllerBase
{
    [HttpPost("createGuestAccount")]
    public async Task<GameResponse<string>> CreateGuestAccount()
    {
        string guestGuid = await accountService.CreateGuestAccountAsync();
        return GameResponse<string>.Ok(guestGuid);
    }

    [HttpPost("createAccount")]
    public async Task<GameResponse> CreateAccount([FromBody] RegisterRequest request)
    {
        await accountService.CreateAccountAsync("", request.Email, request.Password);
        return GameResponse.Ok();
    }
}