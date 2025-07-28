namespace RTWWebServer.DTOs.Request;

public class UpdateNicknameRequest
{
    public long UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
}
