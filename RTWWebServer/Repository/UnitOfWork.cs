using RTWWebServer.Database;

namespace RTWWebServer.Repository;

public class UnitOfWork(AccountDbContext dbContext, IAccountRepository accountRepository, IGuestRepository guestRepository) : IUnitOfWork
{
    private bool _disposed;

    public IAccountRepository Accounts { get; } = accountRepository;
    public IGuestRepository Guests { get; } = guestRepository;

    public async Task<int> CommitAsync()
    {
        return await dbContext.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        await dbContext.Database.CommitTransactionAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        await dbContext.Database.RollbackTransactionAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            dbContext.Dispose();
            _disposed = true;
        }
    }
}