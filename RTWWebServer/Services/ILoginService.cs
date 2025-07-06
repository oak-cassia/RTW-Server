namespace RTWWebServer.Services;

public interface ILoginService
{
    Task<string> LoginAsync(string email, string password);
    Task<string> GuestLoginAsync(string guestGuid);
}