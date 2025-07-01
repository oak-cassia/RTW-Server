namespace RTWWebServer.Entity;

public class UserSession(int userId, string token)
{
    public int UserId { get; set; } = userId;
    public string Token { get; set; } = token;
    public DateTime ExpireTime { get; set; } = DateTime.UtcNow.AddDays(1);
}