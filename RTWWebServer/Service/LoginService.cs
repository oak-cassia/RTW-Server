using NetworkDefinition.ErrorCode;

using RTWWebServer.Database.Repository;

namespace RTWWebServer.Service;

public class LoginService(IAccountRepository accountRepository, ILogger<LoginService> logger) : ILoginService
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly ILogger<LoginService> _logger = logger;

    public async Task<WebServerErrorCode> LoginAsync(string email, string password)
    {
        try
        {
            var account = await _accountRepository.FindByEmail(email);
            if (account == null)
            {
                _logger.LogInformation($"Account with email {email} not found");
                return WebServerErrorCode.AccountNotFound;
            }

            if (account.Password != password)
            {
                _logger.LogInformation($"Password for account with email {email} is incorrect");
                return WebServerErrorCode.InvalidPassword;
            }

            return WebServerErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in LoginAsync: {ex.Message}");
            return WebServerErrorCode.InternalServerError;
        }
    }
}