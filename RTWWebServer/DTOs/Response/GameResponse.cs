using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTOs.Response;

public class GameResponse<T>
{
    public WebServerErrorCode ErrorCode { get; set; }
    public T? Data { get; set; }

    public static GameResponse<T> Ok(T data)
    {
        return new GameResponse<T>
        {
            ErrorCode = WebServerErrorCode.Success,
            Data = data
        };
    }

    public static GameResponse<T> Fail(WebServerErrorCode errorCode)
    {
        return new GameResponse<T>
        {
            ErrorCode = errorCode,
            Data = default
        };
    }
}