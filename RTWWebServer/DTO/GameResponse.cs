using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTO;

public class GameResponse<T>
{
    public WebServerErrorCode ErrorCode { get; set; }
    public T? Data { get; set; }

    public static GameResponse<T> Ok(T data) => new()
    {
        ErrorCode = WebServerErrorCode.Success,
        Data = data
    };

    public static GameResponse<T> Fail(WebServerErrorCode errorCode) => new()
    {
        ErrorCode = errorCode,
        Data = default
    };
}