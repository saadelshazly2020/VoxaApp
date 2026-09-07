namespace VideoChatingApp.WebRTC.Core.Models;

public class UserConnection
{
    public string UserId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public string? CurrentRoomId { get; set; }
    public bool IsInCall { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}
