namespace RTWWebServer.Entity;

public class Account(long id, string userName, string email, string password, string salt)
{
    public long Id { get; set; } = id;
    public string UserName { get; set; } = userName;
    public string Email { get; set; } = email;
    public string Password { get; set; } = password;
    public string Salt { get; set; } = salt;
}