using MySqlConnector;

namespace RTWWebServer.Database;

public interface IMySqlConnectionFactory
{
    MySqlConnection GetAccountConnection();
    MySqlConnection GetGameConnection();
    
    void DisposeConnections();
}