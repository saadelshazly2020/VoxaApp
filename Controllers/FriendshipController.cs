using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoChatingApp.WebRTC.Core.Models;
using VideoChatingApp.WebRTC.Core.Services;

namespace VideoChatingApp.WebRTC.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FriendshipController : ControllerBase
{
    private readonly IFriendshipService _friendshipService;
    private readonly IAuthService _authService;
    private readonly ILogger<FriendshipController> _logger;

    public FriendshipController(IFriendshipService friendshipService, IAuthService authService, ILogger<FriendshipController> logger)
    {
        _friendshipService = friendshipService;
        _authService = authService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpPost("send-request")]
    public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequestRequest request)
    {
        var senderId = GetUserId();
        if (senderId == 0)
            return Unauthorized();

        var (success, message) = await _friendshipService.SendFriendRequestAsync(senderId, request.ReceiverId, request.Message);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("accept-request/{requestId}")]
    public async Task<IActionResult> AcceptRequest(int requestId)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var (success, message) = await _friendshipService.AcceptFriendRequestAsync(requestId);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("reject-request/{requestId}")]
    public async Task<IActionResult> RejectRequest(int requestId)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var (success, message) = await _friendshipService.RejectFriendRequestAsync(requestId);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpDelete("remove/{friendId}")]
    public async Task<IActionResult> RemoveFriend(int friendId)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var (success, message) = await _friendshipService.RemoveFriendAsync(userId, friendId);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetFriends()
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var friends = await _friendshipService.GetFriendsAsync(userId);

        return Ok(new
        {
            friends = friends.Select(f => new
            {
                f.Id,
                f.Username,
                f.DisplayName,
                f.IsOnline,
                f.ProfilePictureUrl,
                f.LastSeen
            })
        });
    }

    [HttpGet("pending-requests")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var requests = await _friendshipService.GetPendingRequestsAsync(userId);

        return Ok(new
        {
            requests = requests.Select(r => new
            {
                r.Id,
                sender = new
                {
                    r.Sender.Id,
                    r.Sender.Username,
                    r.Sender.DisplayName,
                    r.Sender.ProfilePictureUrl
                },
                r.Message,
                r.CreatedAt
            })
        });
    }

    [HttpGet("sent-requests")]
    public async Task<IActionResult> GetSentRequests()
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var requests = await _friendshipService.GetSentRequestsAsync(userId);

        return Ok(new
        {
            requests = requests.Select(r => new
            {
                r.Id,
                receiver = new
                {
                    r.Receiver.Id,
                    r.Receiver.Username,
                    r.Receiver.DisplayName,
                    r.Receiver.ProfilePictureUrl
                },
                r.Message,
                r.CreatedAt
            })
        });
    }

    [HttpGet("search/{query}")]
    public async Task<IActionResult> SearchUsers(string query)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized();

        var users = await _friendshipService.SearchUsersAsync(query, userId);

        var results = new List<object>();
        foreach (var user in users)
        {
            var areFriends = await _friendshipService.AreFriendsAsync(userId, user.Id);
            var pendingRequest = await _friendshipService.GetRequestAsync(userId, user.Id);
            results.Add(new
            {
                user.Id,
                user.Username,
                user.DisplayName,
                user.IsOnline,
                user.ProfilePictureUrl,
                isFriend = areFriends,
                hasPendingRequest = pendingRequest != null
            });
        }

        return Ok(new { users = results });
    }
}

public class SendFriendRequestRequest
{
    public int ReceiverId { get; set; }
    public string? Message { get; set; }
}
