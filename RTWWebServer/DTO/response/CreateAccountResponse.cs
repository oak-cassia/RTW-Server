using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTO.response;

public class CreateAccountResponse(WebServerErrorCode errorCode) : IResponse
{
    public WebServerErrorCode ErrorCode { get; set; } = errorCode;
}