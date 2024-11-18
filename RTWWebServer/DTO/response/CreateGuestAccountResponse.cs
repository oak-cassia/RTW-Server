using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTO.response;

public class CreateGuestAccountResponse(WebServerErrorCode errorCode, string guestGuid) : IResponse
{
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
    public string GuestGuid { get; set; } = guestGuid;
}