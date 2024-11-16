using NetworkDefinition.ErrorCode;
using RTWWebServer.Authentication;
using RTWWebServer.Database.Repository;

namespace RTWWebServer.Service;

public class LoginService(IAccountRepository accountRepository, IPasswordHasher passwordHasher, ILogger<LoginService> logger) : ILoginService
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
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

            var hashedPassword = _passwordHasher.CalcHashedPassword(password, account.Salt);
            if (hashedPassword != account.Password)
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