using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VideoChatingApp.WebRTC.Core.Interfaces;
using VideoChatingApp.WebRTC.Core.Models;
using VideoChatingApp.WebRTC.Data;

namespace VideoChatingApp.WebRTC.Hubs;

public class VideoCallHub : Hub
{
    private readonly IUserManager _userManager;
    private readonly IRoomManager _roomManager;
    private readonly ILogger<VideoCallHub> _logger;
    private readonly IDictionary<int, ICollection<string>> _chatUserConnections;
    private readonly IServiceScopeFactory _scopeFactory;

    public VideoCallHub(
        IUserManager userManager,
        IRoomManager roomManager,
        ILogger<VideoCallHub> logger,
        IDictionary<int, ICollection<string>> chatUserConnections,
        IServiceScopeFactory scopeFactory)
    {
        _userManager = userManager;
        _roomManager = roomManager;
        _logger = logger;
        _chatUserConnections = chatUserConnections;
        _scopeFactory = scopeFactory;
    }

    // Ask whether a user is currently in a call (used by the chat UI)
    public Task<bool> GetCallStatus(int userId)
    {
        return Task.FromResult(_userManager.IsInCall(userId.ToString()));
    }

    // Mark the caller as busy while the call is ringing out
    private async Task SetBusyAsync(string userId, bool busy, bool broadcast = true)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        _userManager.SetCallStatus(userId, busy);

        if (!broadcast || !int.TryParse(userId, out var numericUserId)) return;

        await Clients.All.SendAsync("UserBusyStatusChanged", numericUserId, busy);
    }

    // New method for chat-specific user registration (with database user ID)
    public async Task RegisterChatUser(int userId)
    {
        try
        {
            var connectionId = Context.ConnectionId;

            // Drop this connection from any previous user entry (e.g. re-register)
            RemoveConnectionFromAll(connectionId);

            // Add the connection for this user (a user may have several connections)
            if (!_chatUserConnections.TryGetValue(userId, out var connections))
            {
                connections = new HashSet<string>();
                _chatUserConnections[userId] = connections;
            }
            connections.Add(connectionId);

            await SetUserOnlineStatusAsync(userId, true);

            _logger.LogInformation("Chat user {UserId} registered with connection {ConnectionId}", userId, connectionId);
            
            await Clients.Caller.SendAsync("ChatUserRegistered", userId);
            
            // Notify all online users about this user's status
            await Clients.All.SendAsync("UserOnlineStatusChanged", userId, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chat user registration for {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Chat registration failed");
        }
    }

    public async Task RegisterUser(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Registration attempted with empty userId");
                await Clients.Caller.SendAsync("Error", "UserId cannot be empty");
                return;
            }

            var connectionId = Context.ConnectionId;
            var success = _userManager.AddUser(userId, connectionId);

            if (success)
            {
                _logger.LogInformation("User {UserId} registered with connection {ConnectionId}", userId, connectionId);

                // Notify caller of successful registration
                await Clients.Caller.SendAsync("Registered", userId);

                // Broadcast updated user list to all clients
                await BroadcastUserList();
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Failed to register user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Registration failed");
        }
    }

    public async Task CallUser(string targetUserId, object offer)
    {
        try
        {
            var callerUser = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (callerUser == null)
            {
                _logger.LogWarning("Call attempted by unregistered connection {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "You must register first");
                return;
            }

            var targetConnectionId = _userManager.GetConnectionIdByUserId(targetUserId);
            if (targetConnectionId == null)
            {
                _logger.LogWarning("User {CallerUserId} attempted to call non-existent user {TargetUserId}", 
                    callerUser.UserId, targetUserId);
                await Clients.Caller.SendAsync("Error", "Target user not found");
                return;
            }

            _logger.LogInformation("User {CallerUserId} calling {TargetUserId}", callerUser.UserId, targetUserId);

            // Caller is busy while the call rings / is active
            await SetBusyAsync(callerUser.UserId, true);

            // Notify target user about incoming call (ringing)
            await Clients.Client(targetConnectionId).SendAsync("IncomingCall", callerUser.UserId);

            // Send offer to target user
            await Clients.Client(targetConnectionId).SendAsync("ReceiveOffer", callerUser.UserId, offer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during call to user {TargetUserId}", targetUserId);
            await Clients.Caller.SendAsync("Error", "Call failed");
        }
    }

    public async Task AcceptCall(string callerUserId)
    {
        try
        {
            var calleeUser = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (calleeUser == null)
            {
                _logger.LogWarning("Call acceptance attempted by unregistered connection {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "You must register first");
                return;
            }

            var callerConnectionId = _userManager.GetConnectionIdByUserId(callerUserId);
            if (callerConnectionId == null)
            {
                _logger.LogWarning("User {CalleeUserId} attempted to accept call from non-existent user {CallerUserId}",
                    calleeUser.UserId, callerUserId);
                await Clients.Caller.SendAsync("Error", "Caller not found");
                return;
            }

            _logger.LogInformation("User {CalleeUserId} accepted call from {CallerUserId}", 
                calleeUser.UserId, callerUserId);

            // Both sides are now in a call
            await SetBusyAsync(calleeUser.UserId, true);
            await SetBusyAsync(callerUserId, true);

            // Notify caller that call was accepted
            await Clients.Client(callerConnectionId).SendAsync("CallAccepted", calleeUser.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting call from {CallerUserId}", callerUserId);
            await Clients.Caller.SendAsync("Error", "Failed to accept call");
        }
    }

    public async Task RejectCall(string callerUserId, string reason = "Call rejected")
    {
        try
        {
            var calleeUser = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (calleeUser == null)
            {
                _logger.LogWarning("Call rejection attempted by unregistered connection {ConnectionId}", Context.ConnectionId);
                return;
            }

            var callerConnectionId = _userManager.GetConnectionIdByUserId(callerUserId);
            if (callerConnectionId == null)
            {
                _logger.LogWarning("User {CalleeUserId} attempted to reject call from non-existent user {CallerUserId}",
                    calleeUser.UserId, callerUserId);
                return;
            }

            _logger.LogInformation("User {CalleeUserId} rejected call from {CallerUserId}", 
                calleeUser.UserId, callerUserId);

            // Nobody is in a call anymore
            await SetBusyAsync(calleeUser.UserId, false);
            await SetBusyAsync(callerUserId, false);

            // Notify caller that call was rejected
            await Clients.Client(callerConnectionId).SendAsync("CallRejected", calleeUser.UserId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting call from {CallerUserId}", callerUserId);
        }
    }

    public async Task CancelCall(string targetUserId)
    {
        try
        {
            var callerUser = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (callerUser == null)
            {
                _logger.LogWarning("Call cancellation attempted by unregistered connection {ConnectionId}", Context.ConnectionId);
                return;
            }

            var targetConnectionId = _userManager.GetConnectionIdByUserId(targetUserId);
            if (targetConnectionId == null)
            {
                _logger.LogWarning("User {CallerUserId} attempted to cancel call to non-existent user {TargetUserId}",
                    callerUser.UserId, targetUserId);
                return;
            }

            _logger.LogInformation("User {CallerUserId} cancelled call to {TargetUserId}", 
                callerUser.UserId, targetUserId);

            await SetBusyAsync(callerUser.UserId, false);
            await SetBusyAsync(targetUserId, false);

            // Notify target user that call was cancelled
            await Clients.Client(targetConnectionId).SendAsync("CallCancelled", callerUser.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling call to {TargetUserId}", targetUserId);
        }
    }

    public async Task EndCall(string otherUserId)
    {
        try
        {
            var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (user == null)
            {
                _logger.LogWarning("End call attempted by unregistered connection {ConnectionId}", Context.ConnectionId);
                return;
            }

            var otherConnectionId = _userManager.GetConnectionIdByUserId(otherUserId);
            if (otherConnectionId == null)
            {
                _logger.LogWarning("User {UserId} attempted to end call with non-existent user {OtherUserId}",
                    user.UserId, otherUserId);
                return;
            }

            _logger.LogInformation("User {UserId} ended call with {OtherUserId}", 
                user.UserId, otherUserId);

            await SetBusyAsync(user.UserId, false);
            await SetBusyAsync(otherUserId, false);

            // Notify the other user that call has ended
            await Clients.Client(otherConnectionId).SendAsync("CallEnded", user.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending call with {OtherUserId}", otherUserId);
        }
    }

    public async Task AnswerCall(string callerUserId, object answer)
    {
        try
        {
            var answererUser = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (answererUser == null)
            {
                _logger.LogWarning("Answer attempted by unregistered connection {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "You must register first");
                return;
            }

            var callerConnectionId = _userManager.GetConnectionIdByUserId(callerUserId);
            if (callerConnectionId == null)
            {
                _logger.LogWarning("User {AnswererUserId} attempted to answer non-existent user {CallerUserId}",
                    answererUser.UserId, callerUserId);
                await Clients.Caller.SendAsync("Error", "Caller not found");
                return;
            }

            _logger.LogInformation("User {AnswererUserId} answering call from {CallerUserId}", 
                answererUser.UserId, callerUserId);

            // Send answer to caller
            await Clients.Client(callerConnectionId).SendAsync("ReceiveAnswer", answererUser.UserId, answer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during answer to user {CallerUserId}", callerUserId);
            await Clients.Caller.SendAsync("Error", "Answer failed");
        }
    }

    public async Task SendIceCandidate(string targetUserId, object candidate)
    {
        try
        {
            var senderUser = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (senderUser == null)
            {
                _logger.LogWarning("ICE candidate sent by unregistered connection {ConnectionId}", Context.ConnectionId);
                return;
            }

            var targetConnectionId = _userManager.GetConnectionIdByUserId(targetUserId);
            if (targetConnectionId == null)
            {
                _logger.LogWarning("ICE candidate sent to non-existent user {TargetUserId}", targetUserId);
                return;
            }

            _logger.LogDebug("Forwarding ICE candidate from {SenderUserId} to {TargetUserId}", 
                senderUser.UserId, targetUserId);

            // Forward ICE candidate to target user
            await Clients.Client(targetConnectionId).SendAsync("ReceiveIceCandidate", senderUser.UserId, candidate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ICE candidate to user {TargetUserId}", targetUserId);
        }
    }

    public async Task<string> CreateRoom(string roomId)
    {
        try
        {
            var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (user == null)
            {
                _logger.LogWarning("Room creation attempted by unregistered connection {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "You must register first");
                return string.Empty;
            }

            var room = _roomManager.CreateRoom(roomId, user.UserId);
            _userManager.UpdateUserRoom(user.UserId, roomId);

            _logger.LogInformation("Room {RoomId} created by user {UserId}", roomId, user.UserId);

            await Clients.Caller.SendAsync("RoomCreated", roomId);
            return roomId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room {RoomId}", roomId);
            await Clients.Caller.SendAsync("Error", "Failed to create room");
            return string.Empty;
        }
    }

    public async Task JoinRoom(string roomId)
    {
        try
        {
            var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (user == null)
            {
                _logger.LogWarning("Room join attempted by unregistered connection {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "You must register first");
                return;
            }

            var room = _roomManager.GetRoom(roomId);
            if (room == null)
            {
                _logger.LogWarning("User {UserId} attempted to join non-existent room {RoomId}", 
                    user.UserId, roomId);
                await Clients.Caller.SendAsync("Error", "Room not found");
                return;
            }

            // Get existing participants before adding new user
            var existingParticipants = _roomManager.GetRoomParticipants(roomId).ToList();

            // Add user to room
            _roomManager.AddUserToRoom(roomId, user.UserId);
            _userManager.UpdateUserRoom(user.UserId, roomId);

            _logger.LogInformation("User {UserId} joined room {RoomId}", user.UserId, roomId);

            await SetBusyAsync(user.UserId, true);

            // Notify existing participants about new user
            foreach (var participantUserId in existingParticipants)
            {
                if (participantUserId != user.UserId)
                {
                    var participantConnectionId = _userManager.GetConnectionIdByUserId(participantUserId);
                    if (participantConnectionId != null)
                    {
                        await Clients.Client(participantConnectionId).SendAsync("UserJoinedRoom", user.UserId, roomId);
                    }
                }
            }

            // Send existing participants to the new user
            await Clients.Caller.SendAsync("RoomJoined", roomId, existingParticipants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining room {RoomId}", roomId);
            await Clients.Caller.SendAsync("Error", "Failed to join room");
        }
    }

    public async Task LeaveRoom(string roomId)
    {
        try
        {
            var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (user == null)
            {
                _logger.LogWarning("Room leave attempted by unregistered connection {ConnectionId}", Context.ConnectionId);
                return;
            }

            var room = _roomManager.GetRoom(roomId);
            if (room == null)
            {
                _logger.LogWarning("User {UserId} attempted to leave non-existent room {RoomId}", 
                    user.UserId, roomId);
                return;
            }

            // Remove user from room
            _roomManager.RemoveUserFromRoom(roomId, user.UserId);
            _userManager.UpdateUserRoom(user.UserId, null);

            _logger.LogInformation("User {UserId} left room {RoomId}", user.UserId, roomId);

            await SetBusyAsync(user.UserId, false);

            // Notify other participants
            var remainingParticipants = _roomManager.GetRoomParticipants(roomId);
            foreach (var participantUserId in remainingParticipants)
            {
                var participantConnectionId = _userManager.GetConnectionIdByUserId(participantUserId);
                if (participantConnectionId != null)
                {
                    await Clients.Client(participantConnectionId).SendAsync("UserLeftRoom", user.UserId, roomId);
                }
            }

            await Clients.Caller.SendAsync("RoomLeft", roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving room {RoomId}", roomId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (user != null)
            {
                _logger.LogInformation("User {UserId} disconnecting from connection {ConnectionId}", 
                    user.UserId, Context.ConnectionId);

                // Leave any rooms
                if (!string.IsNullOrEmpty(user.CurrentRoomId))
                {
                    await LeaveRoom(user.CurrentRoomId);
                }

                // Free the user if they were in a call
                if (user.IsInCall)
                {
                    await SetBusyAsync(user.UserId, false, broadcast: false);
                    if (int.TryParse(user.UserId, out var disconnectedUserId))
                        await Clients.All.SendAsync("UserBusyStatusChanged", disconnectedUserId, false);
                }

                // Remove user
                _userManager.RemoveUser(Context.ConnectionId);

                // Broadcast updated user list
                await BroadcastUserList();

                // Notify all clients about disconnection
                await Clients.Others.SendAsync("UserDisconnected", user.UserId);
            }
            
            // Handle chat user disconnection
            var chatUserIds = _chatUserConnections
                .Where(x => x.Value.Contains(Context.ConnectionId))
                .Select(x => x.Key)
                .ToList();

            foreach (var chatUserId in chatUserIds)
            {
                _logger.LogInformation("Chat user {UserId} disconnecting from connection {ConnectionId}",
                    chatUserId, Context.ConnectionId);

                _chatUserConnections[chatUserId].Remove(Context.ConnectionId);

                if (_chatUserConnections[chatUserId].Count == 0)
                {
                    _chatUserConnections.Remove(chatUserId);

                    await SetUserOnlineStatusAsync(chatUserId, false);

                    // Notify all clients about user going offline
                    await Clients.All.SendAsync("UserOnlineStatusChanged", chatUserId, false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnection for connection {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task SetUserOnlineStatusAsync(int userId, bool isOnline)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return;

            user.IsOnline = isOnline;
            user.LastSeen = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating online status for user {UserId}", userId);
        }
    }

    private void RemoveConnectionFromAll(string connectionId)
    {
        var userIds = _chatUserConnections
            .Where(x => x.Value.Contains(connectionId))
            .Select(x => x.Key)
            .ToList();

        foreach (var userId in userIds)
        {
            _chatUserConnections[userId].Remove(connectionId);
            if (_chatUserConnections[userId].Count == 0)
                _chatUserConnections.Remove(userId);
        }
    }

    private async Task BroadcastUserList()
    {
        try
        {
            var users = _userManager.GetAllUsers()
                .Select(u => new { u.UserId, u.CurrentRoomId })
                .ToList();

            await Clients.All.SendAsync("UserListUpdated", users);
            _logger.LogDebug("User list broadcasted to all clients. Count: {Count}", users.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting user list");
        }
    }
}
