using System.Collections.Concurrent;
using VideoChatingApp.WebRTC.Core.Interfaces;
using VideoChatingApp.WebRTC.Core.Models;

namespace VideoChatingApp.WebRTC.Managers;

public class UserManager : IUserManager
{
    private readonly ConcurrentDictionary<string, UserConnection> _usersByConnectionId = new();
    private readonly ConcurrentDictionary<string, string> _connectionIdsByUserId = new();
    private readonly ILogger<UserManager> _logger;

    public UserManager(ILogger<UserManager> logger)
    {
        _logger = logger;
    }

    public bool AddUser(string userId, string connectionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(connectionId))
            {
                _logger.LogWarning("Attempted to add user with invalid userId or connectionId");
                return false;
            }

            // Remove existing connection if user was already connected
            if (_connectionIdsByUserId.TryGetValue(userId, out var existingConnectionId))
            {
                _usersByConnectionId.TryRemove(existingConnectionId, out _);
                _logger.LogInformation("Removed existing connection for user {UserId}", userId);
            }

            var userConnection = new UserConnection
            {
                UserId = userId,
                ConnectionId = connectionId,
                ConnectedAt = DateTime.UtcNow
            };

            _usersByConnectionId[connectionId] = userConnection;
            _connectionIdsByUserId[userId] = connectionId;

            _logger.LogInformation("User {UserId} added with connection {ConnectionId}", userId, connectionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId}", userId);
            return false;
        }
    }

    public bool RemoveUser(string connectionId)
    {
        try
        {
            if (_usersByConnectionId.TryRemove(connectionId, out var userConnection))
            {
                _connectionIdsByUserId.TryRemove(userConnection.UserId, out _);
                _logger.LogInformation("User {UserId} removed with connection {ConnectionId}", 
                    userConnection.UserId, connectionId);
                return true;
            }

            _logger.LogWarning("Attempted to remove non-existent connection {ConnectionId}", connectionId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user with connection {ConnectionId}", connectionId);
            return false;
        }
    }

    public UserConnection? GetUserByConnectionId(string connectionId)
    {
        _usersByConnectionId.TryGetValue(connectionId, out var user);
        return user;
    }

    public UserConnection? GetUserByUserId(string userId)
    {
        if (_connectionIdsByUserId.TryGetValue(userId, out var connectionId))
        {
            return GetUserByConnectionId(connectionId);
        }
        return null;
    }

    public IEnumerable<UserConnection> GetAllUsers()
    {
        return _usersByConnectionId.Values.ToList();
    }

    public string? GetConnectionIdByUserId(string userId)
    {
        _connectionIdsByUserId.TryGetValue(userId, out var connectionId);
        return connectionId;
    }

    public bool SetCallStatus(string userId, bool inCall)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var user = GetUserByUserId(userId);
        if (user == null)
        {
            _logger.LogDebug("Cannot set call status for unknown user {UserId}", userId);
            return false;
        }

        user.IsInCall = inCall;
        return true;
    }

    public bool IsInCall(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var user = GetUserByUserId(userId);
        return user != null && user.IsInCall;
    }

    public bool UpdateUserRoom(string userId, string? roomId)
    {
        try
        {
            var user = GetUserByUserId(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot update room for non-existent user {UserId}", userId);
                return false;
            }

            user.CurrentRoomId = roomId;
            _logger.LogInformation("User {UserId} room updated to {RoomId}", userId, roomId ?? "null");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room for user {UserId}", userId);
            return false;
        }
    }
}
