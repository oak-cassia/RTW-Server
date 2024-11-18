using NetworkDefinition.ErrorCode;

namespace RTWWebServer.RequestResponse;

public class CreateAccountResponse(WebServerErrorCode errorCode) : IResponse
{
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
}