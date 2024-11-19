namespace RTWWebServer.Configuration;

public class DatabaseConfiguration(string accountDatabase, string redis)
{
    public string AccountDatabase { get; set; } = accountDatabase;
    public string Redis { get; set; } = redis;

    public DatabaseConfiguration() : this("", "")
    {
    }
}