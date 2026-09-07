using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VideoChatingApp.WebRTC.Core.Models;
using VideoChatingApp.WebRTC.Core.Services;
using VideoChatingApp.WebRTC.Hubs;

namespace VideoChatingApp.WebRTC.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IHubContext<VideoCallHub> _hubContext;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, IHubContext<VideoCallHub> hubContext, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _hubContext = hubContext;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var senderId = GetUserId();
        if (senderId == 0)
            return Unauthorized();

        var (success, message, chatMessage) = await _chatService.SendMessageAsync(
            senderId,
            request.ReceiverId,
            request.Content,
            request.AttachmentUrl,
            request.AttachmentType
        );

        if (!success)
            return BadRequest(new { message });

        // Send real-time notification to receiver via SignalR
        try
        {
            // Get SignalR connection ID from user connections dictionary
            var connectionDict = HttpContext.RequestServices.GetService<IDictionary<int, ICollection<string>>>();
            if (connectionDict != null &&
                connectionDict.TryGetValue(request.ReceiverId, out var connectionIds) &&
                connectionIds.Count > 0)
            {
                await _hubContext.Clients.Clients(connectionIds.ToList()).SendAsync("ReceiveMessage", new
                {
                    chatMessage!.Id,
                    chatMessage.SenderId,
                    sender = new
                    {
                        chatMessage.Sender.Id,
                        chatMessage.Sender.Username,
                        chatMessage.Sender.DisplayName,
                        chatMessage.Sender.ProfilePictureUrl
                    },
                    chatMessage.ReceiverId,
                    chatMessage.Content,
                    chatMessage.AttachmentUrl,
                    chatMessage.AttachmentType,
                    chatMessage.SentAt,
                    chatMessage.IsRead,
                    IsSentByMe = false
                });
                
                _logger.LogInformation("Real-time message notification sent to user {ReceiverId}", request.ReceiverId);
            }
            else
            {
                _logger.LogInformation("User {ReceiverId} is not connected to SignalR, message will be delivered on next login", request.ReceiverId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send real-time notification to user {ReceiverId}", request.ReceiverId);
        }

        return Ok(new
        {
            message,
            chatMessage = new
            {
                chatMessage!.Id,
                chatMessage.SenderId,
                chatMessage.ReceiverId,
                chatMessage.Content,
                chatMessage.AttachmentUrl,
                chatMessage.AttachmentType,
                chatMessage.SentAt,
                chatMessage.IsRead
            }
        });
    }

    [HttpPost("mark-read/{messageId}")]
    public async Task<IActionResult> MarkAsRead(int messageId)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var (success, message, senderId) = await _chatService.MarkAsReadAsync(messageId, userId);

        if (!success)
            return BadRequest(new { message });

        // Let the sender know their message was read
        await NotifyMessagesRead(userId, senderId, new[] { messageId });

        return Ok(new { message });
    }

    [HttpPost("mark-conversation-read/{otherUserId}")]
    public async Task<IActionResult> MarkConversationAsRead(int otherUserId)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var (success, message) = await _chatService.MarkConversationAsReadAsync(userId, otherUserId);

        if (!success)
            return BadRequest(new { message });

        // Let the other side know the whole conversation was read
        await NotifyMessagesRead(userId, otherUserId, null);

        return Ok(new { message });
    }

    [HttpDelete("delete/{messageId}")]
    public async Task<IActionResult> DeleteMessage(int messageId)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var (success, message) = await _chatService.DeleteMessageAsync(messageId, userId);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    private async Task NotifyMessagesRead(int readerId, int senderId, int[]? messageIds)
    {
        try
        {
            if (senderId == 0) return;

            var connectionDict = HttpContext.RequestServices.GetService<IDictionary<int, ICollection<string>>>();
            if (connectionDict != null &&
                connectionDict.TryGetValue(senderId, out var connectionIds) &&
                connectionIds.Count > 0)
            {
                await _hubContext.Clients.Clients(connectionIds.ToList()).SendAsync("MessagesRead", new
                {
                    readerId,
                    messageIds = messageIds ?? Array.Empty<int>()
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify read receipt to user {SenderId}", senderId);
        }
    }

    [HttpGet("conversation/{otherUserId}")]
    public async Task<IActionResult> GetConversationMessages(int otherUserId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var messages = await _chatService.GetConversationMessagesAsync(userId, otherUserId, skip, take);

        return Ok(new
        {
            messages = messages.Select(m => new
            {
                m.Id,
                m.SenderId,
                sender = new
                {
                    m.Sender.Id,
                    m.Sender.Username,
                    m.Sender.DisplayName,
                    m.Sender.ProfilePictureUrl
                },
                m.ReceiverId,
                m.Content,
                m.AttachmentUrl,
                m.AttachmentType,
                m.SentAt,
                m.IsRead,
                m.ReadAt,
                m.IsDeleted,
                isSentByMe = m.SenderId == userId
            }).Reverse()
        });
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var conversations = await _chatService.GetUserConversationsAsync(userId);

        return Ok(new
        {
            conversations = conversations.Select(c =>
            {
                var otherUser = c.User1Id == userId ? c.User2 : c.User1;
                var unreadCount = c.User1Id == userId ? c.UnreadCountUser1 : c.UnreadCountUser2;
                var lastMessage = c.Messages.FirstOrDefault();

                return new
                {
                    c.Id,
                    otherUser = new
                    {
                        otherUser.Id,
                        otherUser.Username,
                        otherUser.DisplayName,
                        otherUser.ProfilePictureUrl,
                        otherUser.IsOnline,
                        otherUser.LastSeen
                    },
                    lastMessage = lastMessage != null ? new
                    {
                        lastMessage.Content,
                        lastMessage.SentAt,
                        lastMessage.IsRead,
                        lastMessage.AttachmentUrl,
                        isSentByMe = lastMessage.SenderId == userId
                    } : null,
                    unreadCount,
                    c.LastMessageAt
                };
            })
        });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetTotalUnreadCount()
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var count = await _chatService.GetTotalUnreadCountAsync(userId);

        return Ok(new { unreadCount = count });
    }

    [HttpGet("unread-count/{otherUserId}")]
    public async Task<IActionResult> GetUnreadCount(int otherUserId)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var count = await _chatService.GetUnreadCountAsync(userId, otherUserId);

        return Ok(new { unreadCount = count });
    }
}

public class SendMessageRequest
{
    public int ReceiverId { get; set; }
    public string Content { get; set; } = null!;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
}
