namespace RTWWebServer.Data.Entities;

public class PlayerLobbyFurniture(
    long userId,
    int furnitureMasterId,
    int posX,
    int posY,
    int rotation)
{
    public long Id { get; set; }

    public long UserId { get; set; } = userId;

    public int FurnitureMasterId { get; set; } = furnitureMasterId;

    public int PosX { get; set; } = posX;

    public int PosY { get; set; } = posY;

    public int Rotation { get; set; } = rotation;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
