namespace RTWWebServer.DTOs;

public class LobbyInfo
{
    public LobbyFurnitureInfo[] Furniture { get; set; } = [];
}

public class LobbyFurnitureInfo
{
    public long Id { get; set; }
    public int FurnitureMasterId { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int Rotation { get; set; }
    public DateTime UpdatedAt { get; set; }
}
