namespace RTWServer.Game.World.Implementation;

public struct WorldConfiguration(int worldId, int maxPlayers, List<int> mapIds, string worldName)
{
    public int WorldId { get; set; } = worldId;
    public int MaxPlayers { get; set; } = maxPlayers;
    public List<int> MapIds { get; set; } = mapIds;
    public string WorldName { get; set; } = worldName;
    public bool IsDynamic { get; set; } = false; // 동적 생성된 월드인지 구분
}