using System.Collections.Concurrent;
using VideoChatingApp.WebRTC.Core.Interfaces;
using VideoChatingApp.WebRTC.Core.Models;

namespace VideoChatingApp.WebRTC.Managers;

public class RoomManager : IRoomManager
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();
    private readonly ILogger<RoomManager> _logger;

    public RoomManager(ILogger<RoomManager> logger)
    {
        _logger = logger;
    }

    public Room CreateRoom(string roomId, string creatorUserId)
    {
        try
        {
            var room = new Room
            {
                RoomId = roomId,
                CreatorUserId = creatorUserId,
                CreatedAt = DateTime.UtcNow
            };

            room.ParticipantUserIds.Add(creatorUserId);

            if (_rooms.TryAdd(roomId, room))
            {
                _logger.LogInformation("Room {RoomId} created by {CreatorUserId}", roomId, creatorUserId);
                return room;
            }

            // Room already exists, return it
            _logger.LogWarning("Room {RoomId} already exists", roomId);
            return _rooms[roomId];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room {RoomId}", roomId);
            throw;
        }
    }

    public bool AddUserToRoom(string roomId, string userId)
    {
        try
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                bool added = room.ParticipantUserIds.Add(userId);
                if (added)
                {
                    _logger.LogInformation("User {UserId} added to room {RoomId}", userId, roomId);
                }
                return added;
            }

            _logger.LogWarning("Cannot add user {UserId} to non-existent room {RoomId}", userId, roomId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to room {RoomId}", userId, roomId);
            return false;
        }
    }

    public bool RemoveUserFromRoom(string roomId, string userId)
    {
        try
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                bool removed = room.ParticipantUserIds.Remove(userId);
                if (removed)
                {
                    _logger.LogInformation("User {UserId} removed from room {RoomId}", userId, roomId);

                    // Delete room if empty
                    if (room.ParticipantUserIds.Count == 0)
                    {
                        DeleteRoom(roomId);
                    }
                }
                return removed;
            }

            _logger.LogWarning("Cannot remove user {UserId} from non-existent room {RoomId}", userId, roomId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from room {RoomId}", userId, roomId);
            return false;
        }
    }

    public Room? GetRoom(string roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        return room;
    }

    public IEnumerable<string> GetRoomParticipants(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            return room.ParticipantUserIds.ToList();
        }
        return Enumerable.Empty<string>();
    }

    public bool DeleteRoom(string roomId)
    {
        try
        {
            if (_rooms.TryRemove(roomId, out _))
            {
                _logger.LogInformation("Room {RoomId} deleted", roomId);
                return true;
            }

            _logger.LogWarning("Cannot delete non-existent room {RoomId}", roomId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room {RoomId}", roomId);
            return false;
        }
    }

    public IEnumerable<Room> GetAllRooms()
    {
        return _rooms.Values.ToList();
    }
}
