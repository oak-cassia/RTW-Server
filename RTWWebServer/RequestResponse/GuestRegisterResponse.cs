using NetworkDefinition.ErrorCode;

namespace RTWWebServer.RequestResponse;

public class GuestRegisterResponse(WebServerErrorCode errorCode, string guestGuid)
{
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
    public string GuestGuid { get; set; } = guestGuid;
}