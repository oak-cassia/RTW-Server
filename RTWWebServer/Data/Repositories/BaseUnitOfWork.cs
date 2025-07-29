using Microsoft.EntityFrameworkCore;

namespace RTWWebServer.Data.Repositories;

/// <summary>
/// 모든 UnitOfWork 구현체의 기본 클래스
/// </summary>
public abstract class BaseUnitOfWork(DbContext dbContext) : IUnitOfWork
{
    private bool _disposed;

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
        await dbContext.SaveChangesAsync();
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

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            dbContext.Dispose();
            _disposed = true;
        }
    }
}