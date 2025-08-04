namespace RTWWebServer.Data.Repositories;

public class AccountUnitOfWork(AccountDbContext dbContext, IAccountRepository accountRepository) : BaseUnitOfWork(dbContext), IAccountUnitOfWork
{
    public IAccountRepository Accounts { get; } = accountRepository;
}