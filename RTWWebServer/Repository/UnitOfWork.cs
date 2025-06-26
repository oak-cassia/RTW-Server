using RTWWebServer.Database;

namespace RTWWebServer.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly AccountDbContext _dbContext;
    private bool _disposed = false;

    public IAccountRepository Accounts { get; }
    public IGuestRepository Guests { get; }

    public UnitOfWork(AccountDbContext dbContext, IAccountRepository accountRepository, IGuestRepository guestRepository)
    {
        _dbContext = dbContext;
        Accounts = accountRepository;
        Guests = guestRepository;
    }

    public async Task<int> CommitAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        await _dbContext.Database.CommitTransactionAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        await _dbContext.Database.RollbackTransactionAsync();
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
            _dbContext.Dispose();
            _disposed = true;
        }
    }
}