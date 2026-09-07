using VideoChatingApp.WebRTC.Core.Models;

namespace VideoChatingApp.WebRTC.Core.Interfaces;

public interface IRoomManager
{
    Room CreateRoom(string roomId, string creatorUserId);
    bool AddUserToRoom(string roomId, string userId);
    bool RemoveUserFromRoom(string roomId, string userId);
    Room? GetRoom(string roomId);
    IEnumerable<string> GetRoomParticipants(string roomId);
    bool DeleteRoom(string roomId);
    IEnumerable<Room> GetAllRooms();
}
