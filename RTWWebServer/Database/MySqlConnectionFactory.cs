using Microsoft.Extensions.Options;
using MySqlConnector;
using RTWWebServer.Configuration;

namespace RTWWebServer.Database;

public class MySqlConnectionFactory(IOptions<DatabaseConfiguration> configuration) : IMySqlConnectionFactory, IDisposable
{
    private MySqlConnection? _accountConnection;
    private MySqlConnection? _gameConnection;

    public MySqlConnection GetAccountConnection()
    {
        return _accountConnection ??= CreateAccountConnection();
    }

    public MySqlConnection GetGameConnection()
    {
        return _gameConnection ??= CreateGameConnection();
    }

    private MySqlConnection CreateAccountConnection()
    {
        var connection = new MySqlConnection(configuration.Value.AccountDatabase);
        connection.Open();
        return connection;
    }
    
    public void DisposeConnections()
    {
        _accountConnection?.Dispose();
        _accountConnection = null;

        _gameConnection?.Dispose();
        _gameConnection = null;
    }

    private MySqlConnection CreateGameConnection()
    {
        var connection = new MySqlConnection(configuration.Value.GameDatabase);
        connection.Open();
        return connection;
    }

    public void Dispose()
    {
        DisposeConnections();
    }
}