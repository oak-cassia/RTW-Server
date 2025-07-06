using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTOs.Response;

public class GameResponse
{
    public WebServerErrorCode ErrorCode { get; set; }

    public static GameResponse Ok()
    {
        return new GameResponse
        {
            ErrorCode = WebServerErrorCode.Success,
        };
    }

    public static GameResponse Fail(WebServerErrorCode errorCode)
    {
        return new GameResponse
        {
            ErrorCode = errorCode,
        };
    }
}