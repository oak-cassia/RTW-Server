using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTOs.Response;

public class CreateAccountResponse(WebServerErrorCode errorCode) : IResponse
{
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
}