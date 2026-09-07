namespace VideoChatingApp.WebRTC.Core.Models;

public class SignalingMessage
{
    public string Type { get; set; } = string.Empty;
    public string FromUserId { get; set; } = string.Empty;
    public string ToUserId { get; set; } = string.Empty;
    public object? Data { get; set; }
}
