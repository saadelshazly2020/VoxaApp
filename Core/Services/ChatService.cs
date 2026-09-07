using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoChatingApp.WebRTC.Core.Models;
using VideoChatingApp.WebRTC.Data;

namespace VideoChatingApp.WebRTC.Core.Services;

public interface IChatService
{
    Task<(bool Success, string Message, ChatMessage? ChatMessage)> SendMessageAsync(int senderId, int receiverId, string content, string? attachmentUrl = null, string? attachmentType = null);
    Task<(bool Success, string Message, int SenderId)> MarkAsReadAsync(int messageId, int userId);
    Task<(bool Success, string Message)> MarkConversationAsReadAsync(int userId, int otherUserId);
    Task<(bool Success, string Message)> DeleteMessageAsync(int messageId, int userId);
    Task<List<ChatMessage>> GetConversationMessagesAsync(int userId, int otherUserId, int skip = 0, int take = 50);
    Task<List<Conversation>> GetUserConversationsAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId, int otherUserId);
    Task<int> GetTotalUnreadCountAsync(int userId);
    Task<ChatMessage?> GetLastMessageAsync(int userId, int otherUserId);
}

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChatService> _logger;
    private readonly IFriendshipService _friendshipService;

    public ChatService(ApplicationDbContext context, ILogger<ChatService> logger, IFriendshipService friendshipService)
    {
        _context = context;
        _logger = logger;
        _friendshipService = friendshipService;
    }

    public async Task<(bool Success, string Message, ChatMessage? ChatMessage)> SendMessageAsync(
        int senderId, 
        int receiverId, 
        string content, 
        string? attachmentUrl = null, 
        string? attachmentType = null)
    {
        try
        {
            if (senderId == receiverId)
                return (false, "Cannot send message to yourself", null);

            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(attachmentUrl))
                return (false, "Message content or attachment is required", null);

            // Check if users are friends
            var areFriends = await _friendshipService.AreFriendsAsync(senderId, receiverId);
            if (!areFriends)
                return (false, "You can only send messages to friends", null);

            // Get or create conversation
            var conversation = await GetOrCreateConversationAsync(senderId, receiverId);

            // Create message
            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content ?? string.Empty,
                AttachmentUrl = attachmentUrl,
                AttachmentType = attachmentType,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(message);

            // Update conversation
            conversation.LastMessageAt = DateTime.UtcNow;
            if (conversation.User1Id == receiverId)
                conversation.UnreadCountUser1++;
            else
                conversation.UnreadCountUser2++;

            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();

            // Load sender info for the response
            message.Sender = await _context.Users.FindAsync(senderId) ?? null!;

            _logger.LogInformation("Message sent from {SenderId} to {ReceiverId}", senderId, receiverId);
            return (true, "Message sent successfully", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message from {SenderId} to {ReceiverId}", senderId, receiverId);
            return (false, "Error sending message", null);
        }
    }

        public async Task<(bool Success, string Message, int SenderId)> MarkAsReadAsync(int messageId, int userId)
        {
            try
            {
                var message = await _context.ChatMessages.FindAsync(messageId);

                if (message == null)
                    return (false, "Message not found", 0);

                if (message.ReceiverId != userId)
                    return (false, "You can only mark your own messages as read", 0);

                if (message.IsRead)
                    return (true, "Message already marked as read", message.SenderId);

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;

            _context.ChatMessages.Update(message);

            // Update conversation unread count
            var conversation = await GetOrCreateConversationAsync(message.SenderId, message.ReceiverId);
            if (conversation.User1Id == userId && conversation.UnreadCountUser1 > 0)
                conversation.UnreadCountUser1--;
            else if (conversation.User2Id == userId && conversation.UnreadCountUser2 > 0)
                conversation.UnreadCountUser2--;

            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Message {MessageId} marked as read by {UserId}", messageId, userId);
            return (true, "Message marked as read", message.SenderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as read", messageId);
            return (false, "Error marking message as read", 0);
        }
    }

    public async Task<(bool Success, string Message)> MarkConversationAsReadAsync(int userId, int otherUserId)
    {
        try
        {
            var messages = await _context.ChatMessages
                .Where(m => m.ReceiverId == userId && m.SenderId == otherUserId && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            if (messages.Any())
            {
                _context.ChatMessages.UpdateRange(messages);

                // Update conversation unread count
                var conversation = await GetOrCreateConversationAsync(userId, otherUserId);
                if (conversation.User1Id == userId)
                    conversation.UnreadCountUser1 = 0;
                else
                    conversation.UnreadCountUser2 = 0;

                _context.Conversations.Update(conversation);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Conversation between {UserId} and {OtherUserId} marked as read", userId, otherUserId);
            return (true, "Conversation marked as read");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking conversation as read");
            return (false, "Error marking conversation as read");
        }
    }

    public async Task<(bool Success, string Message)> DeleteMessageAsync(int messageId, int userId)
    {
        try
        {
            var message = await _context.ChatMessages.FindAsync(messageId);

            if (message == null)
                return (false, "Message not found");

            if (message.SenderId != userId)
                return (false, "You can only delete your own messages");

            message.IsDeleted = true;
            message.Content = "This message was deleted";

            _context.ChatMessages.Update(message);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Message {MessageId} deleted by {UserId}", messageId, userId);
            return (true, "Message deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
            return (false, "Error deleting message");
        }
    }

    public async Task<List<ChatMessage>> GetConversationMessagesAsync(int userId, int otherUserId, int skip = 0, int take = 50)
    {
        try
        {
            // Return messages in descending order (newest first) so frontend can reverse for display
            // This allows efficient pagination where we can skip the most recent messages
            return await _context.ChatMessages
                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                           (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderByDescending(m => m.SentAt)
                .Skip(skip)
                .Take(take)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation messages");
            return new List<ChatMessage>();
        }
    }

    public async Task<List<Conversation>> GetUserConversationsAsync(int userId)
    {
        try
        {
            return await _context.Conversations
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user conversations");
            return new List<Conversation>();
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId, int otherUserId)
    {
        try
        {
            return await _context.ChatMessages
                .Where(m => m.ReceiverId == userId && m.SenderId == otherUserId && !m.IsRead)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return 0;
        }
    }

    public async Task<int> GetTotalUnreadCountAsync(int userId)
    {
        try
        {
            return await _context.ChatMessages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total unread count");
            return 0;
        }
    }

    public async Task<ChatMessage?> GetLastMessageAsync(int userId, int otherUserId)
    {
        try
        {
            return await _context.ChatMessages
                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                           (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderByDescending(m => m.SentAt)
                .Include(m => m.Sender)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last message");
            return null;
        }
    }

    private async Task<Conversation> GetOrCreateConversationAsync(int userId1, int userId2)
    {
        // Ensure User1Id < User2Id for consistency
        var (user1Id, user2Id) = userId1 < userId2 ? (userId1, userId2) : (userId2, userId1);

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id);

        if (conversation == null)
        {
            conversation = new Conversation
            {
                User1Id = user1Id,
                User2Id = user2Id,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
        }

        return conversation;
    }
}
