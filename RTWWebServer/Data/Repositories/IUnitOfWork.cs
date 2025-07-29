namespace RTWWebServer.Data.Repositories;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveAsync();

    Task BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}