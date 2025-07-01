namespace RTWWebServer.Service;

public interface ILoginService
{
    Task<string> LoginAsync(string email, string password);
    Task<string> GuestLoginAsync(string guestGuid);
}