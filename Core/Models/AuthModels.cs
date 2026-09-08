using System;
using System.Collections.Generic;

namespace VideoChatingApp.WebRTC.Core.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsOnline { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeen { get; set; }

    // Navigation properties
    public ICollection<Friendship> FriendshipsAsUser1 { get; set; } = new List<Friendship>();
    public ICollection<Friendship> FriendshipsAsUser2 { get; set; } = new List<Friendship>();
    public ICollection<FriendshipRequest> SentRequests { get; set; } = new List<FriendshipRequest>();
    public ICollection<FriendshipRequest> ReceivedRequests { get; set; } = new List<FriendshipRequest>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<PostComment> PostComments { get; set; } = new List<PostComment>();
    public ICollection<PostReaction> PostReactions { get; set; } = new List<PostReaction>();
}

public class Friendship
{
    public int Id { get; set; }
    public int User1Id { get; set; }
    public int User2Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User1 { get; set; } = null!;
    public User User2 { get; set; } = null!;
}

public enum FriendshipRequestStatus
{
    Pending,
    Accepted,
    Rejected
}

public class FriendshipRequest
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public FriendshipRequestStatus Status { get; set; } = FriendshipRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public string? Message { get; set; }

    // Navigation properties
    public User Sender { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}
