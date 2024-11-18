namespace RTWWebServer.Service;

public interface IAccountService
{
    Task<bool> CreateAccountAsync(string userName, string email, string password);
    Task<string> CreateGuestAccountAsync();
}