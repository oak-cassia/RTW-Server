using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using NetworkDefinition.ErrorCode;
using RTWWebServer.DTO.response;
using RTWWebServer.Service;

namespace RTWWebServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController(ILogger<AccountController> logger, IAccountService accountService) : ControllerBase

{
    [HttpPost("createGuestAccount")]
    public async Task<CreateGuestAccountResponse> CreateGuestAccount()
    {
        try
        {
            var guestGuid = await accountService.CreateGuestAccountAsync();

            return new CreateGuestAccountResponse(WebServerErrorCode.Success, guestGuid);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to register as guest");
            return new CreateGuestAccountResponse(WebServerErrorCode.InternalServerError, string.Empty);
        }
    }

    [HttpPost("createAccount")]
    public async Task<CreateAccountResponse> CreateAccount([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await accountService.CreateAccountAsync("", request.Email, request.Password);
            return result ? new CreateAccountResponse(WebServerErrorCode.Success) : new CreateAccountResponse(WebServerErrorCode.InternalServerError);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to register");
            return new CreateAccountResponse(WebServerErrorCode.InternalServerError);
        }
    }
}