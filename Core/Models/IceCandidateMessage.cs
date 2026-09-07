namespace VideoChatingApp.WebRTC.Core.Models;

public class IceCandidateMessage
{
    public string Candidate { get; set; } = string.Empty;
    public string SdpMid { get; set; } = string.Empty;
    public int SdpMLineIndex { get; set; }
}
