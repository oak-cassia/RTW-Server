namespace RTWWebServer.Data.Repositories;

public interface IUnitOfWork : IDisposable
{
    Task<int> CommitAsync();

    Task BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}