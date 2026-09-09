using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VideoChatingApp.WebRTC.Core.Models;
using VideoChatingApp.WebRTC.Data;
using VideoChatingApp.WebRTC.Hubs;

namespace VideoChatingApp.WebRTC.Core.Services;

/// <summary>
/// Background timer that nudges participants when a session's scheduled start time arrives.
/// Sends a SignalR "SessionStarting" message (and a push notification fallback) to both the
/// teacher and the student, then flips the session status from Accepted to Ready.
/// </summary>
public class SessionReminderService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<VideoCallHub> _hub;
    private readonly ILogger<SessionReminderService> _logger;
    private Timer? _timer;

    // Notify within a +/- window around the scheduled time so we don't miss any tick
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    public SessionReminderService(
        IServiceScopeFactory scopeFactory,
        IHubContext<VideoCallHub> hub,
        ILogger<SessionReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SessionReminderService starting");
        _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(5), Interval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SessionReminderService stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

            // scheduledAt is stored as naive local time; compare against local now
            var now = DateTime.Now;
            var from = now - Window;
            var to = now + Window;

            var due = await db.TutoringSessions
                .Where(s => s.Status == SessionStatus.Accepted && !s.ReminderSent &&
                            s.ScheduledAt >= from && s.ScheduledAt <= to)
                .ToListAsync();

            if (due.Count == 0) return;

            foreach (var session in due)
            {
                await NotifyAsync(session.TeacherId, session, push);
                await NotifyAsync(session.StudentId, session, push);

                session.ReminderSent = true;
                session.Status = SessionStatus.Ready;
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("SessionReminderService nudged {Count} session(s)", due.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SessionReminderService");
        }
    }

    private async Task NotifyAsync(int userId, TutoringSession session, IPushNotificationService push)
    {
        try
        {
            await _hub.Clients.User(userId.ToString())
                .SendAsync("SessionStarting", session.Id, session.ScheduledAt.ToString("o"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR session-start notification to user {UserId}", userId);
        }

        try
        {
            await push.SendPushAsync(userId, "Session starting",
                "Your tutoring session is about to begin — open Voxa to join.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send push for session-start to user {UserId}", userId);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
