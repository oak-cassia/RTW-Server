using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTOs.Response;

public class UserAuthenticationResponse(WebServerErrorCode errorCode) : IResponse
{
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
}