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
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IHubContext<VideoCallHub> _hubContext;
    private readonly ILogger<PostsController> _logger;

    public PostsController(IPostService postService, IHubContext<VideoCallHub> hubContext, ILogger<PostsController> logger)
    {
        _postService = postService;
        _hubContext = hubContext;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    // ??? Feed ?????????????????????????????????????????????????????????????????

    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var posts = await _postService.GetFeedAsync(userId, skip, take);
        return Ok(new { posts = posts.Select(p => MapPost(p, userId)) });
    }

    // ??? Posts ????????????????????????????????????????????????????????????????

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message, post) = await _postService.CreatePostAsync(userId, request.Content, request.ImageUrl);
        if (!success) return BadRequest(new { message });

        var mapped = MapPost(post!, userId);

        // Notify friends via SignalR
        try
        {
            var connectionDict = GetChatConnections();
            if (connectionDict != null)
            {
                var targetConnections = connectionDict
                    .Where(x => x.Key != userId)
                    .SelectMany(x => x.Value)
                    .ToList();

                if (targetConnections.Count > 0)
                    await _hubContext.Clients.Clients(targetConnections).SendAsync("NewPost", mapped);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast new post {PostId}", post!.Id);
        }

        return Ok(new { message, post = mapped });
    }

    [HttpPut("{postId}")]
    public async Task<IActionResult> EditPost(int postId, [FromBody] EditPostRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message) = await _postService.EditPostAsync(postId, userId, request.Content);
        if (!success) return BadRequest(new { message });

        var post = await _postService.GetPostByIdAsync(postId);
        var mapped = MapPost(post!, userId);

        // Notify via SignalR
        try
        {
            var connectionIds = GetAllConnectionIds();
                    if (connectionIds.Count > 0)
                        await _hubContext.Clients.Clients(connectionIds).SendAsync("PostUpdated", mapped);
        }
        catch { /* non-critical */ }

        return Ok(new { message, post = mapped });
    }

    [HttpDelete("{postId}")]
    public async Task<IActionResult> DeletePost(int postId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message) = await _postService.DeletePostAsync(postId, userId);
        if (!success) return BadRequest(new { message });

        // Notify via SignalR
        try
        {
            var connectionIds = GetAllConnectionIds();
                    if (connectionIds.Count > 0)
                        await _hubContext.Clients.Clients(connectionIds).SendAsync("PostDeleted", postId);
        }
        catch { /* non-critical */ }

        return Ok(new { message });
    }

    // ??? Reactions ????????????????????????????????????????????????????????????

    [HttpPost("{postId}/react")]
    public async Task<IActionResult> ReactToPost(int postId, [FromBody] ReactRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        if (!Enum.TryParse<ReactionType>(request.Type, ignoreCase: true, out var reactionType))
            return BadRequest(new { message = "Invalid reaction type" });

        var (success, message, reaction) = await _postService.ReactToPostAsync(postId, userId, reactionType);
        if (!success) return BadRequest(new { message });

        var payload = new
        {
            postId,
            reaction = new
            {
                reaction!.Id,
                reaction.UserId,
                userName = reaction.User.DisplayName ?? reaction.User.Username,
                type = reaction.Type.ToString()
            }
        };

        // Notify via SignalR
        try
        {
            var connectionIds = GetAllConnectionIds();
                    if (connectionIds.Count > 0)
                        await _hubContext.Clients.Clients(connectionIds).SendAsync("PostReactionUpdated", payload);
        }
        catch { /* non-critical */ }

        return Ok(new { message, reaction = payload.reaction });
    }

    [HttpDelete("{postId}/react")]
    public async Task<IActionResult> RemoveReaction(int postId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message) = await _postService.RemoveReactionAsync(postId, userId);
        if (!success) return BadRequest(new { message });

        // Notify via SignalR
        try
        {
            var connectionIds = GetAllConnectionIds();
                    if (connectionIds.Count > 0)
                        await _hubContext.Clients.Clients(connectionIds).SendAsync("PostReactionRemoved", new { postId, userId });
        }
        catch { /* non-critical */ }

        return Ok(new { message });
    }

    // ??? Comments ?????????????????????????????????????????????????????????????

    [HttpPost("{postId}/comments")]
    public async Task<IActionResult> AddComment(int postId, [FromBody] AddCommentRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message, comment) = await _postService.AddCommentAsync(postId, userId, request.Content);
        if (!success) return BadRequest(new { message });

        var mapped = MapComment(comment!, userId);

        // Notify via SignalR
        try
        {
            var connectionIds = GetAllConnectionIds();
                    if (connectionIds.Count > 0)
                        await _hubContext.Clients.Clients(connectionIds).SendAsync("NewComment", new { postId, comment = mapped });
        }
        catch { /* non-critical */ }

        return Ok(new { message, comment = mapped });
    }

    [HttpGet("{postId}/comments")]
    public async Task<IActionResult> GetComments(int postId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var comments = await _postService.GetCommentsAsync(postId, skip, take);
        return Ok(new { comments = comments.Select(c => MapComment(c, userId)) });
    }

    [HttpPut("comments/{commentId}")]
    public async Task<IActionResult> EditComment(int commentId, [FromBody] AddCommentRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message) = await _postService.EditCommentAsync(commentId, userId, request.Content);
        if (!success) return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message) = await _postService.DeleteCommentAsync(commentId, userId);
        if (!success) return BadRequest(new { message });

        // Notify via SignalR
        try
        {
            var connectionIds = GetAllConnectionIds();
                    if (connectionIds.Count > 0)
                        await _hubContext.Clients.Clients(connectionIds).SendAsync("CommentDeleted", commentId);
        }
        catch { /* non-critical */ }

        return Ok(new { message });
    }

    // ??? SignalR connection helpers ???????????????????????????????????????????

    private IDictionary<int, ICollection<string>>? GetChatConnections()
    {
        return HttpContext.RequestServices.GetService<IDictionary<int, ICollection<string>>>();
    }

    private List<string> GetAllConnectionIds()
    {
        var connectionDict = GetChatConnections();
        return connectionDict == null
            ? new List<string>()
            : connectionDict.SelectMany(x => x.Value).ToList();
    }

    // ??? Mapping helpers ??????????????????????????????????????????????????????

    private static object MapPost(Post p, int currentUserId)
    {
        var myReaction = p.Reactions.FirstOrDefault(r => r.UserId == currentUserId);
        return new
        {
            p.Id,
            p.AuthorId,
            author = new
            {
                p.Author.Id,
                p.Author.Username,
                p.Author.DisplayName,
                p.Author.ProfilePictureUrl
            },
            p.Content,
            p.ImageUrl,
            p.CreatedAt,
            p.UpdatedAt,
            isOwnPost = p.AuthorId == currentUserId,
            myReaction = myReaction != null ? myReaction.Type.ToString() : null,
            reactionCounts = p.Reactions
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            totalReactions = p.Reactions.Count,
            comments = p.Comments
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .Take(3)
                .Select(c => MapComment(c, currentUserId)),
            totalComments = p.Comments.Count(c => !c.IsDeleted)
        };
    }

    private static object MapComment(PostComment c, int currentUserId) => new
    {
        c.Id,
        c.PostId,
        c.AuthorId,
        author = new
        {
            c.Author.Id,
            c.Author.Username,
            c.Author.DisplayName,
            c.Author.ProfilePictureUrl
        },
        c.Content,
        c.CreatedAt,
        c.UpdatedAt,
        isOwnComment = c.AuthorId == currentUserId
    };
}

public class CreatePostRequest
{
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
}

public class EditPostRequest
{
    public string Content { get; set; } = null!;
}

public class ReactRequest
{
    public string Type { get; set; } = null!;
}

public class AddCommentRequest
{
    public string Content { get; set; } = null!;
}
