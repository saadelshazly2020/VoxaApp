using VideoChatingApp.WebRTC.Core.Models;

namespace VideoChatingApp.WebRTC.Core.Interfaces;

public interface IUserManager
{
    bool AddUser(string userId, string connectionId);
    bool RemoveUser(string connectionId);
    UserConnection? GetUserByConnectionId(string connectionId);
    UserConnection? GetUserByUserId(string userId);
    IEnumerable<UserConnection> GetAllUsers();
    string? GetConnectionIdByUserId(string userId);
    bool UpdateUserRoom(string userId, string? roomId);
    bool SetCallStatus(string userId, bool inCall);
    bool IsInCall(string userId);
}
