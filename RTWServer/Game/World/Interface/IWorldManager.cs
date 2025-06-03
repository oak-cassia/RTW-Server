using System.Numerics;
using RTWServer.ServerCore.Interface;
using WorldConfiguration = RTWServer.Game.World.Implementation.WorldConfiguration;

namespace RTWServer.Game.World.Interface;

public interface IWorldManager
{
    IClientSessionManager SessionManager { get; }

    List<IWorld> GetAllWorlds();

    IWorld? GetWorld(int worldId);

    Task<bool> CreateWorldAsync(WorldConfiguration config);

    Task<bool> DestroyWorldAsync(int worldId);

    bool StartWorld(int worldId);

    bool StopWorld(int worldId);

    int? FindAvailableWorld(int preferredWorldId);

    Task<int?> FindOrCreateAvailableWorldAsync(int preferredWorldId);

    Task<bool> AddPlayerToWorldAsync(string sessionId, int worldId, int mapId, Vector3? spawnPosition = null);

    Task<bool> RemovePlayerFromWorldAsync(string sessionId);

    IWorld? GetPlayerWorld(string sessionId);
}