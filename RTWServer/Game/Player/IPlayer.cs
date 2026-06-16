namespace RTWServer.Game.Player;

public interface IPlayer
{
    long PlayerId { get; }
    string SessionId { get; }
    string Name { get; }
}
