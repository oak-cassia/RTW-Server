using NetworkDefinition.ErrorCode;

namespace RTWWebServer.Service;

public interface ILoginService
{
    Task<WebServerErrorCode> LoginAsync(string email, string password);
    
    Task<string> GuestRegisterAsync();
    Task<WebServerErrorCode> GuestLoginAsync(string guestGuid);
}