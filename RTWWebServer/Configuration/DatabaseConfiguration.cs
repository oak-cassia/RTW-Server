namespace RTWWebServer.Configuration;

public class DatabaseConfiguration
{
    public string AccountDatabase { get; set; } = string.Empty;
    
    public DatabaseConfiguration()
    {
    }
    
    public DatabaseConfiguration(string accountDatabase)
    {
        AccountDatabase = accountDatabase;
    }
}