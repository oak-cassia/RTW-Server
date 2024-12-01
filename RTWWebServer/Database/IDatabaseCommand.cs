using MySqlConnector;

namespace RTWWebServer.Database;

public interface IDatabaseCommand : IAsyncDisposable
{
    void AddParameter(string name, object value);
    Task<MySqlDataReader> ExecuteReaderAsync();
    Task<int> ExecuteNonQueryAsync();
}