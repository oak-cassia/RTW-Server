using NetworkDefinition.ErrorCode;

namespace RTWWebServer.Service;

public interface ILoginService
{
    Task<(WebServerErrorCode errorCode, string authToken)> LoginAsync(string email, string password);
    Task<(WebServerErrorCode errorCode, string authToken)> GuestLoginAsync(string guestGuid);
}