using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoChatingApp.WebRTC.Data;
using VideoChatingApp.WebRTC.Core.Models;

namespace VideoChatingApp.WebRTC.Core.Services;

public interface IPushNotificationService
{
    Task SaveSubscriptionAsync(int userId, string endpoint, string p256dh, string auth);
    Task RemoveSubscriptionAsync(string endpoint);
    Task SendPushAsync(int userId, string title, string body, object? data = null);
}

public class PushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly string _vapidPublicKey;
    private readonly string _vapidPrivateKey;
    private readonly string _vapidSubject;

    public PushNotificationService(
        ApplicationDbContext context,
        ILogger<PushNotificationService> logger,
        Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _context = context;
        _logger = logger;
        _vapidPublicKey = config["Vapid:PublicKey"] ?? "";
        _vapidPrivateKey = config["Vapid:PrivateKey"] ?? "";
        _vapidSubject = config["Vapid:Subject"] ?? "mailto:admin@voxa.app";
    }

    public async Task SaveSubscriptionAsync(int userId, string endpoint, string p256dh, string auth)
    {
        try
        {
            // Upsert: remove existing subscription for same endpoint
            var existing = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == endpoint);

            if (existing != null)
            {
                existing.UserId = userId;
                existing.P256dh = p256dh;
                existing.Auth = auth;
                existing.LastUsedAt = DateTime.UtcNow;
            }
            else
            {
                _context.PushSubscriptions.Add(new PushSubscription
                {
                    UserId = userId,
                    Endpoint = endpoint,
                    P256dh = p256dh,
                    Auth = auth,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Push subscription saved for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving push subscription for user {UserId}", userId);
        }
    }

    public async Task RemoveSubscriptionAsync(string endpoint)
    {
        try
        {
            var sub = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == endpoint);
            if (sub != null)
            {
                _context.PushSubscriptions.Remove(sub);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing push subscription");
        }
    }

    public async Task SendPushAsync(int userId, string title, string body, object? data = null)
    {
        if (string.IsNullOrWhiteSpace(_vapidPublicKey) || string.IsNullOrWhiteSpace(_vapidPrivateKey))
        {
            _logger.LogWarning("VAPID keys not configured, skipping push notification");
            return;
        }

        var subscriptions = await _context.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        if (subscriptions.Count == 0) return;

        var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body, data });

        var handler = new WebPush.WebPushClient();
        var vapidDetails = new WebPush.VapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey);

        var failedEndpoints = new List<string>();

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await handler.SendNotificationAsync(pushSub, payload, vapidDetails);
                sub.LastUsedAt = DateTime.UtcNow;
            }
            catch (WebPush.WebPushException ex)
            {
                _logger.LogWarning("Push failed for endpoint (status {StatusCode}): {Message}",
                    ex.StatusCode, ex.Message);

                // Remove invalid subscriptions
                if (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                    ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    failedEndpoints.Add(sub.Endpoint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification");
            }
        }

        if (failedEndpoints.Count > 0)
        {
            var failed = await _context.PushSubscriptions
                .Where(s => failedEndpoints.Contains(s.Endpoint))
                .ToListAsync();
            _context.PushSubscriptions.RemoveRange(failed);
            await _context.SaveChangesAsync();
        }
    }
}
