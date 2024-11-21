using MySqlConnector;

namespace RTWWebServer.Database;

public interface IMySqlConnectionProvider : IDisposable
{
    Task<MySqlConnection> GetAccountConnectionAsync();
    Task<MySqlConnection> GetGameConnectionAsync();
}