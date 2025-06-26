namespace RTWWebServer.Repository;

public interface IUnitOfWork : IDisposable
{
    IAccountRepository Accounts { get; }
    IGuestRepository Guests { get; }
        
    Task<int> CommitAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}