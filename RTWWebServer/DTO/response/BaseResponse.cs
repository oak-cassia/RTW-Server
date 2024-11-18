using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTO.response;

public interface IResponse
{
    public WebServerErrorCode ErrorCode { get; set; }
}