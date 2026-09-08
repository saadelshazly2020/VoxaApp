using System;
using System.ComponentModel.DataAnnotations;

namespace VideoChatingApp.WebRTC.Core.Models;

public class PushSubscription
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? P256dh { get; set; }

    [MaxLength(200)]
    public string? Auth { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }
}
