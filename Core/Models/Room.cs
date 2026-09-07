namespace VideoChatingApp.WebRTC.Core.Models;

public class Room
{
    public string RoomId { get; set; } = string.Empty;
    public HashSet<string> ParticipantUserIds { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatorUserId { get; set; } = string.Empty;
}
