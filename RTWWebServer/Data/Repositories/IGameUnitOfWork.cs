namespace RTWWebServer.Data.Repositories;

public interface IGameUnitOfWork : IUnitOfWork
{
    IUserRepository UserRepository { get; }
}