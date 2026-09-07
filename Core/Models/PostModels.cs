using System;
using System.Collections.Generic;

namespace VideoChatingApp.WebRTC.Core.Models;

public class Post
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public User Author { get; set; } = null!;
    public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
}

public class PostReaction
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
    public ReactionType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class PostComment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int AuthorId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public Post Post { get; set; } = null!;
    public User Author { get; set; } = null!;
}

public enum ReactionType
{
    Like,
    Love,
    Haha,
    Wow,
    Sad,
    Angry
}
