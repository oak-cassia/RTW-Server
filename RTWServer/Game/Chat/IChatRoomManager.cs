namespace RTWServer.Game.Chat;

using RTWServer.Game.Player;

public interface IChatRoomManager
{
    IChatRoom CreateRoom(string roomId, string roomName);
    bool RemoveRoom(string roomId);
    IChatRoom? GetRoom(string roomId);
    IReadOnlyCollection<IChatRoom> GetAllRooms();
    bool JoinRoom(string roomId, IPlayer player);
    bool LeaveRoom(string roomId, string sessionId);
    int LeaveAllRooms(string sessionId);
}