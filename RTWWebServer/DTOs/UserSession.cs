namespace RTWWebServer.DTOs;

public class UserSession(long userId, string token)
{
    public long UserId { get; set; } = userId;
    public string Token { get; set; } = token;
}