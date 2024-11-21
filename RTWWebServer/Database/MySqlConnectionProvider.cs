using System.Data;
using Microsoft.Extensions.Options;
using MySqlConnector;
using RTWWebServer.Configuration;

namespace RTWWebServer.Database;

public class MySqlConnectionProvider(IOptions<DatabaseConfiguration> configuration) : IMySqlConnectionProvider, IAsyncDisposable
{
    private readonly string _accountConnectionString = configuration.Value.AccountDatabase;
    private readonly string _gameConnectionString = configuration.Value.GameDatabase;
    private MySqlConnection? _accountConnection;
    private MySqlConnection? _gameConnection;

    public async Task<MySqlConnection> GetAccountConnectionAsync()
    {
        if (_accountConnection is not { State: ConnectionState.Open })
        {
            _accountConnection = await CreateConnectionAsync(_accountConnectionString);
        }

        return _accountConnection;
    }

    public async Task<MySqlConnection> GetGameConnectionAsync()
    {
        if (_gameConnection is not { State: ConnectionState.Open })
        {
            _gameConnection = await CreateConnectionAsync(_gameConnectionString);
        }

        return _gameConnection;
    }

    private async Task<MySqlConnection> CreateConnectionAsync(string connectionString)
    {
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        return connection;
    }

    public async ValueTask DisposeAsync()
    {
        if (_accountConnection is { State: ConnectionState.Open })
        {
            await _accountConnection.CloseAsync();
            _accountConnection = null;
        }

        if (_gameConnection is { State: ConnectionState.Open })
        {
            await _gameConnection.CloseAsync();
            _gameConnection = null;
        }
    }
}