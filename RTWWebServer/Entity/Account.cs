namespace RTWWebServer.Entity;

public class Account(long id, string userName, string email, string password, string salt)
{
    private Account() : this(0, string.Empty, string.Empty, string.Empty, string.Empty)
    {
    }

    public Account(string username, string email, string password, string salt) : this(0, username, email, password, salt)
    {
    }

    public long Id { get; init; } = id;
    public string UserName { get; private set; } = userName;
    public string Email { get; private set; } = email;
    public string Password { get; private set; } = password;
    public string Salt { get; private set; } = salt;
}