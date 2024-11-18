using NetworkDefinition.ErrorCode;

namespace RTWWebServer.RequestResponse;

public interface IResponse
{
    public WebServerErrorCode ErrorCode { get; set; }
}