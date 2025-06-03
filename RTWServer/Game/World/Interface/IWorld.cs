using System.Numerics;
using RTWServer.Game.Map.Interface;
using RTWServer.Game.Player.Interface;
using RTWServer.Game.World.Implementation;


namespace RTWServer.Game.World.Interface;

public interface IWorld
{
    int WorldId { get; }

    int MaxPlayers { get; }

    int CurrentPlayerCount { get; }

    bool IsActive { get; }

    DateTime CreatedAt { get; }

    Task<bool> InitializeAsync(WorldConfiguration worldConfig);

    bool Start();

    void Stop();

    Task<bool> AddPlayerAsync(string sessionId, int mapId, Vector3? spawnPosition = null);

    Task<bool> RemovePlayerAsync(string sessionId);

    IPlayer? GetPlayer(string sessionId);

    List<IPlayer> GetAllPlayers();

    List<IPlayer> GetPlayersOnMap(int mapId);

    IMap? GetMap(int mapId);
}