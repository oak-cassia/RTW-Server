namespace RTWWebServer.Database.Data;

public class UserSession(int userId, string authToken)
{
    public int UserId { get; set; } = userId;
    public string AuthToken { get; set; } = authToken;
}