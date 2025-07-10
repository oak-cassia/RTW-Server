namespace RTWWebServer.Data.Repositories;

public interface IAccountUnitOfWork : IUnitOfWork
{
    IAccountRepository Accounts { get; }
    IGuestRepository Guests { get; }
}