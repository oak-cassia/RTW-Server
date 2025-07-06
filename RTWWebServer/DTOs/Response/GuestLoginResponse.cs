using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTOs.Response;

public class GuestLoginResponse(WebServerErrorCode errorCode, string authToken) : IResponse
{
    public string AuthToken { get; set; } = authToken;
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
}