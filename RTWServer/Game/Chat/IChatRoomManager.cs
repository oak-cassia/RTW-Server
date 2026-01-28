namespace RTWServer.Game.Chat;

public interface IChatRoomManager
{
    IChatRoom CreateRoom(string roomId, string roomName);
    bool RemoveRoom(string roomId);
    IChatRoom? GetRoom(string roomId);
    IReadOnlyCollection<IChatRoom> GetAllRooms();
}
