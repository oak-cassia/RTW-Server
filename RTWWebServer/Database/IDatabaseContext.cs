using MySqlConnector;

namespace RTWWebServer.Database;

public interface IDatabaseContext : IDisposable
{
    Task<IDatabaseCommand> CreateCommandAsync(string query);

    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}