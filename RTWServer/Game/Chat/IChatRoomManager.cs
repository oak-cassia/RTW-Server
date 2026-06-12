namespace RTWServer.Game.Chat;

using RTWServer.Game.Player;

public interface IChatRoomManager
{
    /// <summary>
    /// 방을 가져오거나 새로 만듭니다. roomId가 유효하지 않거나 방 개수 제한을 넘으면 null을 반환합니다.
    /// isPersistent가 true인 방은 비어 있어도 자동으로 제거되지 않습니다.
    /// </summary>
    IChatRoom? GetOrCreateRoom(string roomId, string roomName, bool isPersistent = false);

    bool RemoveRoom(string roomId);
    IChatRoom? GetRoom(string roomId);
    IReadOnlyCollection<IChatRoom> GetAllRooms();
    bool JoinRoom(string roomId, IPlayer player);
    bool LeaveRoom(string roomId, string sessionId);
    int LeaveAllRooms(string sessionId);
}
