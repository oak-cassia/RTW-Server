using MySqlConnector;

namespace RTWWebServer.Database;

public interface IMySqlConnectionProvider
{
    Task<MySqlConnection> GetAccountConnectionAsync();
    Task<MySqlConnection> GetGameConnectionAsync();
}