using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTOs.Response;

public interface IResponse
{
    public WebServerErrorCode ErrorCode { get; set; }
}