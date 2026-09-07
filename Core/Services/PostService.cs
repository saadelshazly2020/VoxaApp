using Microsoft.EntityFrameworkCore;
using VideoChatingApp.WebRTC.Core.Models;
using VideoChatingApp.WebRTC.Data;

namespace VideoChatingApp.WebRTC.Core.Services;

public interface IPostService
{
    Task<(bool Success, string Message, Post? Post)> CreatePostAsync(int authorId, string content, string? imageUrl);
    Task<(bool Success, string Message)> DeletePostAsync(int postId, int userId);
    Task<(bool Success, string Message)> EditPostAsync(int postId, int userId, string newContent);
    Task<List<Post>> GetFeedAsync(int userId, int skip = 0, int take = 20);
    Task<Post?> GetPostByIdAsync(int postId);
    Task<(bool Success, string Message, PostReaction? Reaction)> ReactToPostAsync(int postId, int userId, ReactionType type);
    Task<(bool Success, string Message)> RemoveReactionAsync(int postId, int userId);
    Task<(bool Success, string Message, PostComment? Comment)> AddCommentAsync(int postId, int authorId, string content);
    Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId);
    Task<(bool Success, string Message)> EditCommentAsync(int commentId, int userId, string newContent);
    Task<List<PostComment>> GetCommentsAsync(int postId, int skip = 0, int take = 20);
}

public class PostService : IPostService
{
    private readonly ApplicationDbContext _context;
    private readonly IFriendshipService _friendshipService;
    private readonly ILogger<PostService> _logger;

    public PostService(ApplicationDbContext context, IFriendshipService friendshipService, ILogger<PostService> logger)
    {
        _context = context;
        _friendshipService = friendshipService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, Post? Post)> CreatePostAsync(int authorId, string content, string? imageUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(imageUrl))
                return (false, "Post must have content or an image", null);

            if (content.Length > 2000)
                return (false, "Post content cannot exceed 2000 characters", null);

            var post = new Post
            {
                AuthorId = authorId,
                Content = content.Trim(),
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            post.Author = (await _context.Users.FindAsync(authorId))!;

            _logger.LogInformation("Post {PostId} created by user {UserId}", post.Id, authorId);
            return (true, "Post created", post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post for user {UserId}", authorId);
            return (false, "Failed to create post", null);
        }
    }

    public async Task<(bool Success, string Message)> DeletePostAsync(int postId, int userId)
    {
        try
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null || post.IsDeleted)
                return (false, "Post not found");

            if (post.AuthorId != userId)
                return (false, "You can only delete your own posts");

            post.IsDeleted = true;
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Post {PostId} deleted by user {UserId}", postId, userId);
            return (true, "Post deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", postId);
            return (false, "Failed to delete post");
        }
    }

    public async Task<(bool Success, string Message)> EditPostAsync(int postId, int userId, string newContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newContent))
                return (false, "Content cannot be empty");

            if (newContent.Length > 2000)
                return (false, "Post content cannot exceed 2000 characters");

            var post = await _context.Posts.FindAsync(postId);
            if (post == null || post.IsDeleted)
                return (false, "Post not found");

            if (post.AuthorId != userId)
                return (false, "You can only edit your own posts");

            post.Content = newContent.Trim();
            post.UpdatedAt = DateTime.UtcNow;
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Post {PostId} edited by user {UserId}", postId, userId);
            return (true, "Post updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing post {PostId}", postId);
            return (false, "Failed to update post");
        }
    }

    public async Task<List<Post>> GetFeedAsync(int userId, int skip = 0, int take = 20)
    {
        try
        {
            // Get friend IDs
            var friends = await _friendshipService.GetFriendsAsync(userId);
            var friendIds = friends.Select(f => f.Id).ToList();

            // Include the user's own posts
            friendIds.Add(userId);

            return await _context.Posts
                .Where(p => friendIds.Contains(p.AuthorId) && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(p => p.Author)
                .Include(p => p.Reactions)
                    .ThenInclude(r => r.User)
                .Include(p => p.Comments.Where(c => !c.IsDeleted).OrderBy(c => c.CreatedAt))
                    .ThenInclude(c => c.Author)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feed for user {UserId}", userId);
            return new List<Post>();
        }
    }

    public async Task<Post?> GetPostByIdAsync(int postId)
    {
        return await _context.Posts
            .Where(p => p.Id == postId && !p.IsDeleted)
            .Include(p => p.Author)
            .Include(p => p.Reactions)
                .ThenInclude(r => r.User)
            .Include(p => p.Comments.Where(c => !c.IsDeleted).OrderBy(c => c.CreatedAt))
                .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, PostReaction? Reaction)> ReactToPostAsync(int postId, int userId, ReactionType type)
    {
        try
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null || post.IsDeleted)
                return (false, "Post not found", null);

            var existing = await _context.PostReactions
                .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);

            if (existing != null)
            {
                // Update reaction type
                existing.Type = type;
                existing.CreatedAt = DateTime.UtcNow;
                _context.PostReactions.Update(existing);
                await _context.SaveChangesAsync();
                existing.User = (await _context.Users.FindAsync(userId))!;
                return (true, "Reaction updated", existing);
            }

            var reaction = new PostReaction
            {
                PostId = postId,
                UserId = userId,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };

            _context.PostReactions.Add(reaction);
            await _context.SaveChangesAsync();

            reaction.User = (await _context.Users.FindAsync(userId))!;
            _logger.LogInformation("User {UserId} reacted to post {PostId}", userId, postId);
            return (true, "Reaction added", reaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reacting to post {PostId}", postId);
            return (false, "Failed to react", null);
        }
    }

    public async Task<(bool Success, string Message)> RemoveReactionAsync(int postId, int userId)
    {
        try
        {
            var reaction = await _context.PostReactions
                .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);

            if (reaction == null)
                return (false, "Reaction not found");

            _context.PostReactions.Remove(reaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} removed reaction from post {PostId}", userId, postId);
            return (true, "Reaction removed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing reaction from post {PostId}", postId);
            return (false, "Failed to remove reaction");
        }
    }

    public async Task<(bool Success, string Message, PostComment? Comment)> AddCommentAsync(int postId, int authorId, string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
                return (false, "Comment cannot be empty", null);

            if (content.Length > 1000)
                return (false, "Comment cannot exceed 1000 characters", null);

            var post = await _context.Posts.FindAsync(postId);
            if (post == null || post.IsDeleted)
                return (false, "Post not found", null);

            var comment = new PostComment
            {
                PostId = postId,
                AuthorId = authorId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.PostComments.Add(comment);
            await _context.SaveChangesAsync();

            comment.Author = (await _context.Users.FindAsync(authorId))!;
            _logger.LogInformation("Comment {CommentId} added to post {PostId} by user {UserId}", comment.Id, postId, authorId);
            return (true, "Comment added", comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to post {PostId}", postId);
            return (false, "Failed to add comment", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId)
    {
        try
        {
            var comment = await _context.PostComments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null || comment.IsDeleted)
                return (false, "Comment not found");

            // Allow comment author or post author to delete
            if (comment.AuthorId != userId && comment.Post.AuthorId != userId)
                return (false, "You cannot delete this comment");

            comment.IsDeleted = true;
            _context.PostComments.Update(comment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Comment {CommentId} deleted by user {UserId}", commentId, userId);
            return (true, "Comment deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
            return (false, "Failed to delete comment");
        }
    }

    public async Task<(bool Success, string Message)> EditCommentAsync(int commentId, int userId, string newContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newContent))
                return (false, "Content cannot be empty");

            if (newContent.Length > 1000)
                return (false, "Comment cannot exceed 1000 characters");

            var comment = await _context.PostComments.FindAsync(commentId);
            if (comment == null || comment.IsDeleted)
                return (false, "Comment not found");

            if (comment.AuthorId != userId)
                return (false, "You can only edit your own comments");

            comment.Content = newContent.Trim();
            comment.UpdatedAt = DateTime.UtcNow;
            _context.PostComments.Update(comment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Comment {CommentId} edited by user {UserId}", commentId, userId);
            return (true, "Comment updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing comment {CommentId}", commentId);
            return (false, "Failed to update comment");
        }
    }

    public async Task<List<PostComment>> GetCommentsAsync(int postId, int skip = 0, int take = 20)
    {
        try
        {
            return await _context.PostComments
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(c => c.Author)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
            return new List<PostComment>();
        }
    }
}
