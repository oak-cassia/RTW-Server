namespace RTWWebServer.Data.Repositories;

public class GameUnitOfWork(GameDbContext dbContext, IUserRepository userRepository) : BaseUnitOfWork(dbContext), IGameUnitOfWork
{
    public IUserRepository UserRepository { get; } = userRepository;
}