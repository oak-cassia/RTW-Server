namespace RTWServer.Game.Player;

public interface IPlayer
{
    int PlayerId { get; }
    string SessionId { get; }
    string Name { get; }
}
