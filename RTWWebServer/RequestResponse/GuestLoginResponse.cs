using NetworkDefinition.ErrorCode;

namespace RTWWebServer.RequestResponse;

public class GuestLoginResponse(WebServerErrorCode errorCode) : IResponse
{
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
}