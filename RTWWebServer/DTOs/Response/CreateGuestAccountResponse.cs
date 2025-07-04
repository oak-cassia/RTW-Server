using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTOs.Response;

public class CreateGuestAccountResponse(WebServerErrorCode errorCode, string guestGuid) : IResponse
{
    public string GuestGuid { get; set; } = guestGuid;
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
}