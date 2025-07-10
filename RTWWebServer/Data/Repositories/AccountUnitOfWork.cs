namespace RTWWebServer.Data.Repositories;

public class AccountUnitOfWork(AccountDbContext dbContext, IAccountRepository accountRepository, IGuestRepository guestRepository) : BaseUnitOfWork(dbContext), IAccountUnitOfWork
{
    public IAccountRepository Accounts { get; } = accountRepository;
    public IGuestRepository Guests { get; } = guestRepository;
}