namespace RTWWebServer.Data.Entities;

// 유저별 방 상태. 현재는 방 크기를 결정하는 등급만 보관한다(가구 배치는 PlayerLobbyFurniture).
// 행이 없는 유저는 1등급(기본)으로 간주하므로, 확장 시점에만 행을 lazy-create 한다.
public class PlayerLobby(
    long userId,
    int roomGrade)
{
    public long Id { get; set; }

    public long UserId { get; set; } = userId;

    public int RoomGrade { get; set; } = roomGrade;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
