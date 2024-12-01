using MySqlConnector;

namespace RTWWebServer.Database;

public class MySqlDatabaseCommand(MySqlCommand command) : IDatabaseCommand
{
    public void AddParameter(string name, object value)
    {
        command.Parameters.AddWithValue(name, value);
    }

    public async Task<MySqlDataReader> ExecuteReaderAsync()
    {
        return await command.ExecuteReaderAsync();
    }

    public async Task<int> ExecuteNonQueryAsync()
    {
        return await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await command.DisposeAsync();
    }
}