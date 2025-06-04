using System.Collections.Concurrent;
using System.Numerics;
using Microsoft.Extensions.Logging;
using RTWServer.Game.Player.Interface;
using RTWServer.Game.World.Interface;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.World.Implementation;

public class WorldManager : IWorldManager
{
    private readonly ConcurrentDictionary<int, IWorld> _worlds = new();
    private readonly ConcurrentDictionary<string, int> _playerToWorldMapping = new();
    private readonly IClientSessionManager _sessionManager;
    private readonly IWorldFactory _worldFactory;
    private readonly ILogger<WorldManager> _logger;
    private readonly WorldConfiguration _defaultWorldTemplate;

    private readonly SemaphoreSlim _dynamicWorldCreationSemaphore = new(1, 1);

    public IClientSessionManager SessionManager => _sessionManager;

    public WorldManager(IClientSessionManager sessionManager, IWorldFactory worldFactory, WorldConfiguration defaultWorldTemplate, ILoggerFactory loggerFactory)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _worldFactory = worldFactory ?? throw new ArgumentNullException(nameof(worldFactory));
        _defaultWorldTemplate = defaultWorldTemplate;
        _logger = loggerFactory.CreateLogger<WorldManager>();

        _logger.LogInformation("WorldManager initialized");
    }

    public List<IWorld> GetAllWorlds()
    {
        return _worlds.Values.ToList();
    }

    public IWorld? GetWorld(int worldId)
    {
        _worlds.TryGetValue(worldId, out IWorld? world);
        return world;
    }

    public async Task<bool> CreateWorldAsync(WorldConfiguration config)
    {
        IWorld? world = null;
        try
        {
            world = _worldFactory.CreateWorld(config);

            bool initResult = await world.InitializeAsync(config);
            if (!initResult)
            {
                _logger.LogError("Failed to initialize world {WorldId}", config.WorldId);
                world.Stop();
                return false;
            }

            if (!_worlds.TryAdd(config.WorldId, world))
            {
                _logger.LogWarning("World with ID {WorldId} already exists", config.WorldId);
                world.Stop();
                return false;
            }

            _logger.LogInformation("World {WorldId} ({WorldName}) created successfully with max players: {MaxPlayers}", config.WorldId, config.WorldName, config.MaxPlayers);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while creating world {WorldId}", config.WorldId);
            world?.Stop();
            return false;
        }
    }

    public async Task<bool> DestroyWorldAsync(int worldId)
    {
        try
        {
            if (!_worlds.TryRemove(worldId, out IWorld? worldToDestroy))
            {
                _logger.LogWarning("Cannot destroy non-existent world {WorldId}", worldId);
                return false;
            }

            List<IPlayer> playersInWorld = worldToDestroy.GetAllPlayers();
            _logger.LogInformation("Found {PlayerCount} players in world {WorldId} to be destroyed", playersInWorld.Count, worldId);

            foreach (IPlayer player in playersInWorld)
            {
                string sessionId = player.SessionId;

                await worldToDestroy.RemovePlayerAsync(sessionId);

                _playerToWorldMapping.TryRemove(sessionId, out _);

                _logger.LogInformation("Disconnecting player {SessionId} due to world destruction", sessionId);
                await _sessionManager.InitiateClientDisconnectAsync(sessionId, $"World {worldId} destroyed");
            }

            worldToDestroy.Stop();

            _logger.LogInformation("World {WorldId} destroyed successfully", worldId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while destroying world {WorldId}", worldId);
            return false;
        }
    }

    public bool StartWorld(int worldId)
    {
        try
        {
            if (!_worlds.TryGetValue(worldId, out IWorld? world))
            {
                _logger.LogWarning("Cannot start non-existent world {WorldId}", worldId);
                return false;
            }

            // World는 내부적으로 atomic하게 start 상태를 관리한다고 가정
            bool result = world.Start();

            _logger.LogInformation("World {WorldId} start operation completed with result: {Result}",
                worldId, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while starting world {WorldId}", worldId);
            return false;
        }
    }

    public bool StopWorld(int worldId)
    {
        try
        {
            // TryRemove로 수정할 수도 있음
            if (!_worlds.TryGetValue(worldId, out IWorld? world))
            {
                _logger.LogWarning("Cannot stop non-existent world {WorldId}", worldId);
                return false;
            }

            // World는 내부적으로 atomic하게 stop 상태를 관리한다고 가정
            world.Stop();

            _logger.LogInformation("World {WorldId} stop operation completed", worldId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while stopping world {WorldId}", worldId);
            return false;
        }
    }

    public int? FindAvailableWorld(int preferredWorldId)
    {
        try
        {
            // 선호하는 월드가 사용 가능한지 먼저 확인
            if (_worlds.TryGetValue(preferredWorldId, out IWorld? preferredWorld))
            {
                if (preferredWorld.IsAvailableForNewPlayer())
                {
                    _logger.LogDebug("Preferred world {WorldId} is available", preferredWorldId);
                    return preferredWorldId;
                }
            }

            // 사용 가능한 다른 월드 찾기
            foreach (IWorld world in _worlds.Values)
            {
                if (!world.IsAvailableForNewPlayer())
                {
                    continue;
                }

                _logger.LogDebug("Found available world {WorldId} (players: {CurrentPlayers}/{MaxPlayers})", world.WorldId, world.CurrentPlayerCount, world.MaxPlayers);
                return world.WorldId;
            }

            _logger.LogWarning("No available worlds found (preferred: {PreferredWorldId})", preferredWorldId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while finding available world (preferred: {PreferredWorldId})", preferredWorldId);
            return null;
        }
    }

    public async Task<bool> AddPlayerToWorldAsync(string sessionId, int worldId, int mapId, Vector3? spawnPosition = null)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("Cannot add player with null or empty session ID to world {WorldId}", worldId);
                return false;
            }

            // 세션이 유효한지 확인
            IClientSession? session = _sessionManager.GetClientSession(sessionId);
            if (session == null)
            {
                _logger.LogWarning("Cannot add player: session {SessionId} not found", sessionId);
                return false;
            }

            if (!session.IsAuthenticated)
            {
                _logger.LogWarning("Cannot add unauthenticated player {SessionId} to world {WorldId}", sessionId, worldId);
                return false;
            }

            // 이미 다른 월드에 있는지 확인
            if (_playerToWorldMapping.TryGetValue(sessionId, out int currentWorldId))
            {
                _logger.LogWarning("Player {SessionId} is already in world {CurrentWorldId}", sessionId, currentWorldId);
                return false;
            }

            // 대상 월드 확인
            if (!_worlds.TryGetValue(worldId, out IWorld? world))
            {
                _logger.LogWarning("Cannot add player to non-existent world {WorldId}", worldId);
                return false;
            }

            if (!world.IsActive)
            {
                _logger.LogWarning("Cannot add player to inactive world {WorldId}", worldId);
                return false;
            }

            // 월드에 플레이어 추가
            bool addResult = await world.AddPlayerAsync(sessionId, mapId, spawnPosition);
            if (addResult)
            {
                // 플레이어-월드 매핑 추가
                _playerToWorldMapping.TryAdd(sessionId, worldId);

                _logger.LogInformation("Player {SessionId} added to world {WorldId} on map {MapId}", sessionId, worldId, mapId);
                return true;
            }

            _logger.LogError("Failed to add player {SessionId} to world {WorldId}", sessionId, worldId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while adding player {SessionId} to world {WorldId}", sessionId, worldId);
            return false;
        }
    }

    public async Task<bool> RemovePlayerFromWorldAsync(string sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("Cannot remove player with null or empty session ID");
                return false;
            }

            // 플레이어가 속한 월드 찾기 및 매핑에서 제거
            if (!_playerToWorldMapping.TryRemove(sessionId, out int worldId))
            {
                _logger.LogWarning("Player {SessionId} is not in any world", sessionId);
                return false;
            }

            // 월드에서 플레이어 제거
            if (_worlds.TryGetValue(worldId, out IWorld? world))
            {
                bool removeResult = await world.RemovePlayerAsync(sessionId);
                if (removeResult)
                {
                    _logger.LogInformation("Player {SessionId} removed from world {WorldId}", sessionId, worldId);
                    return true;
                }

                _logger.LogError("Failed to remove player {SessionId} from world {WorldId}", sessionId, worldId);
                return false;
            }

            _logger.LogWarning("World {WorldId} not found while removing player {SessionId}", worldId, sessionId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while removing player {SessionId}", sessionId);
            return false;
        }
    }

    public IWorld? GetPlayerWorld(string sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return null;
            }

            if (!_playerToWorldMapping.TryGetValue(sessionId, out int worldId))
            {
                return null;
            }

            _worlds.TryGetValue(worldId, out IWorld? world);
            return world;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while getting world for player {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<int?> FindOrCreateAvailableWorldAsync(int preferredWorldId)
    {
        try
        {
            // 1. 첫 번째 체크: Lock 없이 빠르게 확인
            int? availableWorldId = FindAvailableWorld(preferredWorldId);
            if (availableWorldId != null)
            {
                return availableWorldId;
            }

            // 2. Lock을 사용한 동적 생성 (Double-checked locking 패턴)
            await _dynamicWorldCreationSemaphore.WaitAsync();
            try
            {
                // Lock 내에서 다시 한번 확인 (다른 스레드가 이미 생성했을 수 있음)
                availableWorldId = FindAvailableWorld(preferredWorldId);
                if (availableWorldId != null)
                {
                    _logger.LogDebug("Another thread created world while waiting for lock");
                    return availableWorldId;
                }

                // 실제로 새 월드 생성이 필요함
                _logger.LogInformation("All worlds are full, creating new dynamic world");

                int newWorldId = GenerateNewWorldId();
                WorldConfiguration newWorldConfig = new WorldConfiguration
                {
                    WorldId = newWorldId,
                    WorldName = $"{_defaultWorldTemplate.WorldName} {newWorldId}",
                    MaxPlayers = _defaultWorldTemplate.MaxPlayers,
                    MapIds = new List<int>(_defaultWorldTemplate.MapIds),
                    IsDynamic = true
                };

                bool created = await CreateWorldAsync(newWorldConfig);
                if (!created)
                {
                    _logger.LogError("Failed to create dynamic world {WorldId}", newWorldId);
                    return null;
                }

                bool started = StartWorld(newWorldId);
                if (started)
                {
                    _logger.LogInformation("Dynamically created and started world {WorldId}", newWorldId);
                    return newWorldId;
                }

                // 시작 실패 시 생성된 월드 정리
                await DestroyWorldAsync(newWorldId);
                _logger.LogError("Failed to start dynamically created world {WorldId}", newWorldId);
                return null;
            }
            finally
            {
                _dynamicWorldCreationSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while finding or creating available world");
            return null;
        }
    }

    private int GenerateNewWorldId()
    {
        // 현재 존재하는 월드 ID 중 최대값 + 1
        // 동적 월드는 템플릿 WorldId를 시작 번호로 사용
        int minWorldId = _defaultWorldTemplate.WorldId;
        var maxId = _worlds.Keys.Where(id => id >= minWorldId).DefaultIfEmpty(minWorldId - 1).Max();
        return maxId + 1;
    }
}