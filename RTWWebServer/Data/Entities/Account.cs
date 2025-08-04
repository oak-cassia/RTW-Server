using RTWWebServer.Enums;

namespace RTWWebServer.Data.Entities;

public class Account
{
    public Account(string guid, UserRole role, DateTime createdAt, DateTime updatedAt)
    {
        Guid = guid;
        Role = role;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Account(string email, string password, string salt, UserRole role, DateTime createdAt, DateTime updatedAt)
    {
        Email = email;
        Password = password;
        Salt = salt;
        Role = role;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public long Id { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Salt { get; set; }
    public string? Guid { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}