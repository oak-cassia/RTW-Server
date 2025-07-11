namespace RTWWebServer.Configuration;

public class DatabaseConfiguration(string accountDatabase, string gameDatabase, string redis)
{
    public DatabaseConfiguration() : this("", "", "")
    {
    }

    public string AccountDatabase { get; set; } = accountDatabase;
    public string GameDatabase { get; set; } = gameDatabase;
    public string Redis { get; set; } = redis;
}