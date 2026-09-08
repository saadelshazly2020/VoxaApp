using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoChatingApp.WebRTC.Core.Models;
using VideoChatingApp.WebRTC.Data;

namespace VideoChatingApp.WebRTC.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(ApplicationDbContext context, ILogger<ProfileController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.DisplayName,
            user.ProfilePictureUrl,
            user.Bio,
            user.CreatedAt
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.DisplayName = request.DisplayName;

        if (request.Bio != null)
            user.Bio = request.Bio.Length > 500 ? request.Bio[..500] : request.Bio;

        if (request.ProfilePictureUrl != null)
            user.ProfilePictureUrl = request.ProfilePictureUrl;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Profile updated for user {UserId}", userId);

        return Ok(new
        {
            message = "Profile updated",
            user = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.DisplayName,
                user.ProfilePictureUrl,
                user.Bio
            }
        });
    }

    [HttpGet("{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserProfile(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var currentUserId = GetUserId();
        bool isFriend = false;
        if (currentUserId > 0)
        {
            isFriend = await _context.Friendships.AnyAsync(f =>
                (f.User1Id == currentUserId && f.User2Id == userId) ||
                (f.User1Id == userId && f.User2Id == currentUserId));
        }

        var postCount = await _context.Posts.CountAsync(p => p.AuthorId == userId && !p.IsDeleted);

        return Ok(new
        {
            user.Id,
            user.Username,
            user.DisplayName,
            user.ProfilePictureUrl,
            user.Bio,
            user.IsOnline,
            user.LastSeen,
            user.CreatedAt,
            isFriend,
            postCount
        });
    }
}

public class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
