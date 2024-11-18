using NetworkDefinition.ErrorCode;

namespace RTWWebServer.Service;

public interface ILoginService
{
    Task<WebServerErrorCode> LoginAsync(string email, string password);
    Task<WebServerErrorCode> GuestLoginAsync(string guestGuid);
}