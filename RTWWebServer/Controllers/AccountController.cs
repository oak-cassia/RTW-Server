using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using NetworkDefinition.ErrorCode;
using RTWWebServer.DTOs.Response;
using RTWWebServer.Services;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController(IAccountService accountService) : ControllerBase
{
    [HttpPost("createGuestAccount")]
    public async Task<CreateGuestAccountResponse> CreateGuestAccount()
    {
        string guestGuid = await accountService.CreateGuestAccountAsync();
        return new CreateGuestAccountResponse(WebServerErrorCode.Success, guestGuid);
    }

    [HttpPost("createAccount")]
    public async Task<CreateAccountResponse> CreateAccount([FromBody] RegisterRequest request)
    {
        await accountService.CreateAccountAsync("", request.Email, request.Password);
        return new CreateAccountResponse(WebServerErrorCode.Success);
    }
}