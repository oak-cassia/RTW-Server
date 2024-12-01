using System.Data;
using MySqlConnector;

namespace RTWWebServer.Database;

public class MySqlDatabaseContext(string connectionConfigString) : IDatabaseContext
{
    private MySqlConnection? _connection;

    private MySqlTransaction? _transaction;

    public async Task<MySqlCommand> CreateCommandAsync(string query)
    {
        MySqlConnection connection = await GetConnectionAsync();
        MySqlCommand command = new MySqlCommand(query, connection);

        if (_transaction is not null)
        {
            command.Transaction = _transaction;
        }

        return command;
    }

    public async Task BeginTransactionAsync()
    {
        Task<MySqlConnection> connection = GetConnectionAsync();
        _transaction = await connection.Result.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("Transaction is not started");
        }

        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();

        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("Transaction is not started");
        }

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();

        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Close();
    }

    private async Task<MySqlConnection> GetConnectionAsync()
    {
        if (_connection is not { State: ConnectionState.Open })
        {
            _connection = await CreateConnectionAsync(connectionConfigString);
        }

        return _connection;
    }

    private async Task<MySqlConnection> CreateConnectionAsync(string connectionString)
    {
        MySqlConnection connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        return connection;
    }
}