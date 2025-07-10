namespace RTWWebServer.Data.Entities;

public class User(long id, string? guid, string? email, int userType, string? nickname, DateTime createdAt, DateTime updatedAt)
{
    private User()
        : this(0, string.Empty, string.Empty, 0, string.Empty, default, default)
    {
    }

    public User(string? guid, string? email, int userType, string? nickname, DateTime createdAt, DateTime updatedAt)
        : this(0, guid, email, userType, nickname, createdAt, updatedAt)
    {
    }

    public long Id { get; set; } = id;
    public string? Guid { get; set; } = guid;
    public string? Email { get; set; } = email;
    public int UserType { get; set; } = userType;
    public string? Nickname { get; set; } = nickname;
    public DateTime CreatedAt { get; set; } = createdAt;
    public DateTime UpdatedAt { get; set; } = updatedAt;
}