namespace RTWServer.Game.Player;

public class Player : IPlayer
{
    public int PlayerId { get; }
    public string SessionId { get; }
    public string Name { get; }
    
    public Player(int playerId, string sessionId, string name)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("SessionId cannot be null or whitespace.", nameof(sessionId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
        }

        PlayerId = playerId;
        SessionId = sessionId;
        Name = name;
    }
}
