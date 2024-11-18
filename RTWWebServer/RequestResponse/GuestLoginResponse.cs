using NetworkDefinition.ErrorCode;

namespace RTWWebServer.RequestResponse;

public class GuestLoginResponse(WebServerErrorCode errorCode)
{
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
}