namespace RTWWebServer.DTOs;

public class UserSession(int userId, string token)
{
    public int UserId { get; set; } = userId;
    public string Token { get; set; } = token;
}