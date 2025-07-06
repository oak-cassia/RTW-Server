using NetworkDefinition.ErrorCode;

namespace RTWWebServer.DTOs.Response;

public class GameEntryResponse
{
    public WebServerErrorCode ErrorCode { get; set; }
    public string AuthToken { get; set; } = string.Empty;
    public int UserId { get; set; }

    public GameEntryResponse(WebServerErrorCode errorCode, string authToken = "", int userId = 0)
    {
        ErrorCode = errorCode;
        AuthToken = authToken;
        UserId = userId;
    }
}
