using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VideoChatingApp.WebRTC.Core.Services;

namespace VideoChatingApp.WebRTC.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _authService = authService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, message, userId) = await _authService.RegisterAsync(request.Username, request.Email, request.Password);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message, userId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, message, _, userId) = await _authService.LoginAsync(request.Email, request.Password);

        if (!success)
            return Unauthorized(new { message });

        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var token = _authService.GenerateJwtToken(user, _configuration);

        return Ok(new
        {
            message,
            token,
            user = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.DisplayName,
                user.ProfilePictureUrl,
                user.Bio,
                user.IsOnline
            }
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var user = await _authService.GetUserByIdAsync(request.UserId);
        if (user == null)
            return NotFound();

        var loggedOut = await _authService.LogoutAsync(request.UserId);
        if (!loggedOut)
            return BadRequest(new { message = "Failed to log out" });

        return Ok(new { message = "Logged out successfully" });
    }
}

public class RegisterRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LogoutRequest
{
    public int UserId { get; set; }
}
