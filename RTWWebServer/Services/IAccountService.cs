namespace RTWWebServer.Services;

public interface IAccountService
{
    Task CreateAccountAsync(string userName, string email, string password);
    Task<string> CreateGuestAccountAsync();
}