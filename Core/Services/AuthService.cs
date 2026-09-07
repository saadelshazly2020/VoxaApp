using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using VideoChatingApp.WebRTC.Core.Models;
using VideoChatingApp.WebRTC.Data;

namespace VideoChatingApp.WebRTC.Core.Services;

public interface IAuthService
{
    Task<(bool Success, string Message, int? UserId)> RegisterAsync(string username, string email, string password);
    Task<(bool Success, string Message, string? Token, int? UserId)> LoginAsync(string email, string password);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> LogoutAsync(int userId);
    Task<bool> SetOnlineStatusAsync(int userId, bool isOnline);
    bool VerifyPassword(string password, string hash);
    string HashPassword(string password);
    string GenerateJwtToken(User user, IConfiguration config);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ApplicationDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, int? UserId)> RegisterAsync(string username, string email, string password)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return (false, "Username, email, and password are required", null);

            if (password.Length < 6)
                return (false, "Password must be at least 6 characters", null);

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email || u.Username == username);

            if (existingUser != null)
                return (false, "Email or username already exists", null);

            // Create new user
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                DisplayName = username,
                IsOnline = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} registered successfully", username);
            return (true, "Registration successful", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", email);
            return (false, "Registration failed", null);
        }
    }

    public async Task<(bool Success, string Message, string? Token, int? UserId)> LoginAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return (false, "Email and password are required", null, null);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Trim().ToLower() == email.Trim().ToLower());

            if (user == null || !VerifyPassword(password, user.PasswordHash))
                return (false, "Invalid email or password", null, null);

            // Update online status
            user.IsOnline = true;
            user.LastSeen = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Email} logged in successfully", email);
            return (true, "Login successful", null, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", email);
            return (false, "Login failed", null, null);
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.FriendshipsAsUser1)
            .Include(u => u.FriendshipsAsUser2)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> LogoutAsync(int userId)
    {
        return await SetOnlineStatusAsync(userId, false);
    }

    public async Task<bool> SetOnlineStatusAsync(int userId, bool isOnline)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.IsOnline = isOnline;
            user.LastSeen = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating online status for user {UserId}", userId);
            return false;
        }
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public string GenerateJwtToken(User user, IConfiguration config)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "your-secret-key-change-this-in-production-environment"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim("userId", user.Id.ToString()),
            new System.Security.Claims.Claim("email", user.Email),
            new System.Security.Claims.Claim("username", user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"] ?? "VideoChatingApp",
            audience: config["Jwt:Audience"] ?? "VideoChatingAppUsers",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
