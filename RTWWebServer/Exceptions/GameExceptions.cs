using NetworkDefinition.ErrorCode;

namespace RTWWebServer.Exceptions;

public class GameException(string message, WebServerErrorCode errorCode) : Exception(message)
{
    public WebServerErrorCode ErrorCode { get; } = errorCode;
}