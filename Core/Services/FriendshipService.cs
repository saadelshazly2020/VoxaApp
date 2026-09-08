using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoChatingApp.WebRTC.Core.Models;
using VideoChatingApp.WebRTC.Data;

namespace VideoChatingApp.WebRTC.Core.Services;

public interface IFriendshipService
{
    Task<(bool Success, string Message)> SendFriendRequestAsync(int senderId, int receiverId, string? message = null);
    Task<(bool Success, string Message)> AcceptFriendRequestAsync(int requestId);
    Task<(bool Success, string Message)> RejectFriendRequestAsync(int requestId);
    Task<(bool Success, string Message)> RemoveFriendAsync(int userId, int friendId);
    Task<List<User>> GetFriendsAsync(int userId);
    Task<List<FriendshipRequest>> GetPendingRequestsAsync(int userId);
    Task<List<FriendshipRequest>> GetSentRequestsAsync(int userId);
    Task<bool> AreFriendsAsync(int userId1, int userId2);
    Task<FriendshipRequest?> GetRequestAsync(int senderId, int receiverId);
    Task<List<User>> SearchUsersAsync(string query, int currentUserId);
}

public class FriendshipService : IFriendshipService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FriendshipService> _logger;

    public FriendshipService(ApplicationDbContext context, ILogger<FriendshipService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SendFriendRequestAsync(int senderId, int receiverId, string? message = null)
    {
        try
        {
            if (senderId == receiverId)
                return (false, "You cannot send a friend request to yourself");

            var sender = await _context.Users.FindAsync(senderId);
            var receiver = await _context.Users.FindAsync(receiverId);

            if (sender == null || receiver == null)
                return (false, "User not found");

            // Check if already friends
            var areFriends = await AreFriendsAsync(senderId, receiverId);
            if (areFriends)
                return (false, "You are already friends");

            // Check for existing request
            var existingRequest = await _context.FriendshipRequests
                .FirstOrDefaultAsync(fr => 
                    (fr.SenderId == senderId && fr.ReceiverId == receiverId && fr.Status == FriendshipRequestStatus.Pending) ||
                    (fr.SenderId == receiverId && fr.ReceiverId == senderId && fr.Status == FriendshipRequestStatus.Pending));

            if (existingRequest != null)
                return (false, "A friendship request already exists between you and this user");

            // Create new request
            var friendshipRequest = new FriendshipRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                Status = FriendshipRequestStatus.Pending
            };

            _context.FriendshipRequests.Add(friendshipRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Friend request sent from {SenderId} to {ReceiverId}", senderId, receiverId);
            return (true, "Friend request sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend request from {SenderId} to {ReceiverId}", senderId, receiverId);
            return (false, "Error sending friend request");
        }
    }

    public async Task<(bool Success, string Message)> AcceptFriendRequestAsync(int requestId)
    {
        try
        {
            var request = await _context.FriendshipRequests.FindAsync(requestId);

            if (request == null)
                return (false, "Request not found");

            if (request.Status != FriendshipRequestStatus.Pending)
                return (false, "Request is no longer pending");

            // Create friendship
            var friendship = new Friendship
            {
                User1Id = Math.Min(request.SenderId, request.ReceiverId),
                User2Id = Math.Max(request.SenderId, request.ReceiverId)
            };

            // Update request
            request.Status = FriendshipRequestStatus.Accepted;
            request.RespondedAt = DateTime.UtcNow;

            _context.Friendships.Add(friendship);
            _context.FriendshipRequests.Update(request);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Friend request {RequestId} accepted", requestId);
            return (true, "Friend request accepted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting friend request {RequestId}", requestId);
            return (false, "Error accepting friend request");
        }
    }

    public async Task<(bool Success, string Message)> RejectFriendRequestAsync(int requestId)
    {
        try
        {
            var request = await _context.FriendshipRequests.FindAsync(requestId);

            if (request == null)
                return (false, "Request not found");

            if (request.Status != FriendshipRequestStatus.Pending)
                return (false, "Request is no longer pending");

            request.Status = FriendshipRequestStatus.Rejected;
            request.RespondedAt = DateTime.UtcNow;

            _context.FriendshipRequests.Update(request);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Friend request {RequestId} rejected", requestId);
            return (true, "Friend request rejected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting friend request {RequestId}", requestId);
            return (false, "Error rejecting friend request");
        }
    }

    public async Task<(bool Success, string Message)> RemoveFriendAsync(int userId, int friendId)
    {
        try
        {
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => 
                    (f.User1Id == userId && f.User2Id == friendId) ||
                    (f.User1Id == friendId && f.User2Id == userId));

            if (friendship == null)
                return (false, "Friendship not found");

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Friendship between {UserId} and {FriendId} removed", userId, friendId);
            return (true, "Friend removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing friend {FriendId} for user {UserId}", friendId, userId);
            return (false, "Error removing friend");
        }
    }

    public async Task<List<User>> GetFriendsAsync(int userId)
    {
        try
        {
            var friendships = await _context.Friendships
                .Where(f => f.User1Id == userId || f.User2Id == userId)
                .Include(f => f.User1)
                .Include(f => f.User2)
                .ToListAsync();

            var friends = friendships
                .Select(f => f.User1Id == userId ? f.User2 : f.User1)
                .ToList();

            return friends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting friends for user {UserId}", userId);
            return new List<User>();
        }
    }

    public async Task<List<FriendshipRequest>> GetPendingRequestsAsync(int userId)
    {
        try
        {
            return await _context.FriendshipRequests
                .Where(fr => fr.ReceiverId == userId && fr.Status == FriendshipRequestStatus.Pending)
                .Include(fr => fr.Sender)
                .Include(fr => fr.Receiver)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending requests for user {UserId}", userId);
            return new List<FriendshipRequest>();
        }
    }

    public async Task<List<FriendshipRequest>> GetSentRequestsAsync(int userId)
    {
        try
        {
            return await _context.FriendshipRequests
                .Where(fr => fr.SenderId == userId && fr.Status == FriendshipRequestStatus.Pending)
                .Include(fr => fr.Sender)
                .Include(fr => fr.Receiver)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sent requests for user {UserId}", userId);
            return new List<FriendshipRequest>();
        }
    }

    public async Task<bool> AreFriendsAsync(int userId1, int userId2)
    {
        return await _context.Friendships
            .AnyAsync(f => 
                (f.User1Id == userId1 && f.User2Id == userId2) ||
                (f.User1Id == userId2 && f.User2Id == userId1));
    }

    public async Task<FriendshipRequest?> GetRequestAsync(int senderId, int receiverId)
    {
        return await _context.FriendshipRequests
            .FirstOrDefaultAsync(fr => 
                fr.SenderId == senderId && 
                fr.ReceiverId == receiverId && 
                fr.Status == FriendshipRequestStatus.Pending);
    }

    public async Task<List<User>> SearchUsersAsync(string query, int currentUserId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<User>();

            var lowerQuery = query.ToLower();
            return await _context.Users
                .Where(u => u.Id != currentUserId &&
                    (u.Username.ToLower().Contains(lowerQuery) ||
                     (u.DisplayName != null && u.DisplayName.ToLower().Contains(lowerQuery))))
                .Take(10)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with query {Query}", query);
            return new List<User>();
        }
    }
}
