namespace RTWWebServer.DTOs;

public class UserSession(long userId, string token, string nickname)
{
    public long UserId { get; set; } = userId;
    public string Token { get; set; } = token;
    public string Nickname { get; set; } = nickname;
}