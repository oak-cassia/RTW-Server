namespace RTWWebServer.Data.Repositories;

public class GameUnitOfWork(GameDbContext dbContext, IUserRepository userRepository, IPlayerCharacterRepository playerCharacterRepository) : BaseUnitOfWork(dbContext), IGameUnitOfWork
{
    public IUserRepository UserRepository { get; } = userRepository;
    public IPlayerCharacterRepository PlayerCharacterRepository { get; } = playerCharacterRepository;
}