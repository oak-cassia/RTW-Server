using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTO.response;

public class UserAuthenticationResponse(WebServerErrorCode errorCode) : IResponse
{
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
}