using MySqlConnector;

namespace RTWWebServer.Database;

public interface IDatabaseContext : IDisposable
{
    Task<MySqlCommand> CreateCommandAsync(string query);

    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}