using NetworkDefinition.ErrorCode;
using RTWWebServer.Authentication;
using RTWWebServer.Database.Repository;

namespace RTWWebServer.Service;

public class LoginService(
    IAccountRepository accountRepository,
    IPasswordHasher passwordHasher,
    IGuidGenerator guidGenerator,
    IGuestRepository guestRepository,
    ILogger<LoginService> logger) : ILoginService
{
    public async Task<WebServerErrorCode> LoginAsync(string email, string password)
    {
        try
        {
            var account = await accountRepository.FindByEmailAsync(email);
            if (account == null)
            {
                logger.LogInformation($"Account with email {email} not found");
                return WebServerErrorCode.AccountNotFound;
            }

            var hashedPassword = passwordHasher.CalcHashedPassword(password, account.Salt);
            if (hashedPassword != account.Password)
            {
                logger.LogInformation($"Password for account with email {email} is incorrect");
                return WebServerErrorCode.InvalidPassword;
            }

            return WebServerErrorCode.Success;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error in LoginAsync: {ex.Message}");
            return WebServerErrorCode.InternalServerError;
        }
    }

    public async Task<string> GuestRegisterAsync()
    {
        try
        {
            var guid = guidGenerator.GenerateGuid();
            logger.LogInformation($"Generated guid {guid.ToString()}");
            await guestRepository.CreateGuestAsync(guid.ToByteArray());
            return guid.ToString();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to register as guest");
            return string.Empty;
        }
    }

    public async Task<WebServerErrorCode> GuestLoginAsync(string guestGuid)
    {
        try
        {
            var guest = await guestRepository.FindByGuidAsync(Guid.Parse(guestGuid).ToByteArray());
            if (guest == null)
            {
                logger.LogInformation($"Guest with guid {guestGuid} not found");
                return WebServerErrorCode.GuestNotFound;
            }

            if (guestGuid != guest.Guid.ToString())
            {
                logger.LogInformation($"Guid for guest with guid {guestGuid} is incorrect. Expected {guest.Guid.ToString()}");
                return WebServerErrorCode.GuestNotFound;
            }

            return WebServerErrorCode.Success;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to login as guest");
            return WebServerErrorCode.InternalServerError;
        }
    }
}