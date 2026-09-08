using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoChatingApp.WebRTC.Core.Services;

namespace VideoChatingApp.WebRTC.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PushNotificationController : ControllerBase
{
    private readonly IPushNotificationService _pushService;
    private readonly IConfiguration _config;

    public PushNotificationController(IPushNotificationService pushService, IConfiguration config)
    {
        _pushService = pushService;
        _config = config;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        await _pushService.SaveSubscriptionAsync(userId, request.Endpoint, request.P256dh, request.Auth);
        return Ok(new { message = "Subscribed" });
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        await _pushService.RemoveSubscriptionAsync(request.Endpoint);
        return Ok(new { message = "Unsubscribed" });
    }

    [HttpGet("vapid-public-key")]
    [AllowAnonymous]
    public IActionResult GetVapidPublicKey()
    {
        var key = _config["Vapid:PublicKey"];
        if (string.IsNullOrWhiteSpace(key))
            return Ok(new { enabled = false });
        return Ok(new { enabled = true, publicKey = key });
    }
}

public class PushSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}

public class UnsubscribeRequest
{
    public string Endpoint { get; set; } = string.Empty;
}
