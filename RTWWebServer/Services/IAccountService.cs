namespace RTWWebServer.Services;

public interface IAccountService
{
    Task CreateAccountAsync(string email, string password);
    Task<string> CreateGuestAccountAsync();
}