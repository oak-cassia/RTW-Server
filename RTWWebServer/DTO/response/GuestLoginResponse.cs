using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTO.response;

public class GuestLoginResponse(WebServerErrorCode errorCode, string authToken) : IResponse
{
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
    public string AuthToken { get; set; } = authToken;
}