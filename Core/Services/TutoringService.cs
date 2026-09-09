using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoChatingApp.WebRTC.Core.Models;
using VideoChatingApp.WebRTC.Data;

namespace VideoChatingApp.WebRTC.Core.Services;

public interface ITutoringService
{
    // Subjects
    Task<List<SubjectTag>> GetSubjectsAsync();
    Task<SubjectTag?> GetSubjectByIdAsync(int id);

    // Teacher profile
    Task<TeacherProfile?> GetTeacherProfileAsync(int userId);
    Task<TeacherProfile?> GetTeacherProfileByTeacherIdAsync(int teacherProfileId);
    Task<List<TeacherProfile>> SearchTeachersAsync(int? subjectId, string? query, bool acceptingOnly = true);
    Task<(bool Success, string Message, TeacherProfile? Profile)> CreateOrUpdateTeacherProfileAsync(
        int userId, string? hourlyRate, int experienceYears, List<int> subjectIds);
    Task<(bool Success, string Message)> SetAvailabilityAsync(
        int teacherProfileId, List<AvailabilitySlotInput> slots);
    Task<(bool Success, string Message)> ToggleAcceptingStudentsAsync(int teacherProfileId, bool accepting);

    // Sessions
    Task<(bool Success, string Message, TutoringSession? Session)> CreateSessionAsync(
        int studentId, int teacherProfileId, int? subjectId, DateTime scheduledAt, int durationMinutes, string? notes);
    Task<(bool Success, string Message)> RespondToSessionAsync(
        int teacherId, int sessionId, bool accept, string? teacherNotes = null);
    Task<(bool Success, string Message)> CancelSessionAsync(int userId, int sessionId, string? reason = null);
    Task<(bool Success, string Message)> CompleteSessionAsync(int teacherId, int sessionId, string? teacherNotes = null);
    Task<(bool Success, string Message)> StartSessionAsync(int userId, int sessionId);
    Task<List<TutoringSession>> GetUserSessionsAsync(int userId, SessionStatus? status = null, bool asTeacher = true);
    Task<TutoringSession?> GetSessionAsync(int sessionId);
    Task<List<TutoringSession>> GetUpcomingSessionsAsync(int userId);

    // Reviews
    Task<(bool Success, string Message)> CreateReviewAsync(
        int reviewerId, int sessionId, int rating, string? comment);
    Task<List<TutoringReview>> GetTeacherReviewsAsync(int teacherProfileId, int limit = 10);
    Task<double?> GetTeacherAverageRatingAsync(int teacherProfileId);
}

public class TutoringService : ITutoringService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TutoringService> _logger;

    public TutoringService(ApplicationDbContext context, ILogger<TutoringService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ─── Subjects ────────────────────────────────────────────

    public async Task<List<SubjectTag>> GetSubjectsAsync()
        => await _context.SubjectTags.OrderBy(s => s.Name).ToListAsync();

    public async Task<SubjectTag?> GetSubjectByIdAsync(int id)
        => await _context.SubjectTags.FindAsync(id);

    // ─── Teacher Profile ─────────────────────────────────────

    public async Task<TeacherProfile?> GetTeacherProfileAsync(int userId)
        => await _context.TeacherProfiles
            .Include(tp => tp.User)
            .Include(tp => tp.Subjects).ThenInclude(ts => ts.SubjectTag)
            .Include(tp => tp.AvailabilitySlots)
            .FirstOrDefaultAsync(tp => tp.UserId == userId);

    public async Task<TeacherProfile?> GetTeacherProfileByTeacherIdAsync(int teacherProfileId)
        => await _context.TeacherProfiles
            .Include(tp => tp.User)
            .Include(tp => tp.Subjects).ThenInclude(ts => ts.SubjectTag)
            .Include(tp => tp.AvailabilitySlots)
            .FirstOrDefaultAsync(tp => tp.Id == teacherProfileId);

    public async Task<List<TeacherProfile>> SearchTeachersAsync(
        int? subjectId, string? query, bool acceptingOnly = true)
    {
        var q = _context.TeacherProfiles
            .Include(tp => tp.User)
            .Include(tp => tp.Subjects).ThenInclude(ts => ts.SubjectTag)
            .AsQueryable();

        if (acceptingOnly)
            q = q.Where(tp => tp.IsAcceptingStudents);

        if (subjectId.HasValue)
            q = q.Where(tp => tp.Subjects.Any(ts => ts.SubjectTagId == subjectId.Value));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.ToLower();
            q = q.Where(tp =>
                tp.User.DisplayName != null && tp.User.DisplayName.ToLower().Contains(lower) ||
                tp.User.Username.ToLower().Contains(lower) ||
                tp.Subjects.Any(ts => ts.SubjectTag.Name.ToLower().Contains(lower)));
        }

        return await q.OrderByDescending(tp => tp.IsAcceptingStudents)
                       .ThenBy(tp => tp.User.DisplayName)
                       .ToListAsync();
    }

    public async Task<(bool Success, string Message, TeacherProfile? Profile)> CreateOrUpdateTeacherProfileAsync(
        int userId, string? hourlyRate, int experienceYears, List<int> subjectIds)
    {
        try
        {
            // Ensure user exists and promote to Teacher
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "User not found", null);

            user.Role = UserRole.Teacher;

            var profile = await _context.TeacherProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == userId);

            if (profile == null)
            {
                profile = new TeacherProfile
                {
                    UserId = userId,
                    HourlyRate = hourlyRate,
                    ExperienceYears = experienceYears,
                    IsAcceptingStudents = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.TeacherProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }
            else
            {
                profile.HourlyRate = hourlyRate;
                profile.ExperienceYears = experienceYears;
            }

            // Sync subjects
            var existing = await _context.TeacherSubjects
                .Where(ts => ts.TeacherProfileId == profile.Id)
                .ToListAsync();

            _context.TeacherSubjects.RemoveRange(existing);

            foreach (var subjectId in subjectIds.Distinct())
            {
                if (await _context.SubjectTags.AnyAsync(s => s.Id == subjectId))
                {
                    _context.TeacherSubjects.Add(new TeacherSubject
                    {
                        TeacherProfileId = profile.Id,
                        SubjectTagId = subjectId
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Reload with navigation
            profile = await _context.TeacherProfiles
                .Include(tp => tp.User)
                .Include(tp => tp.Subjects).ThenInclude(ts => ts.SubjectTag)
                .FirstOrDefaultAsync(tp => tp.Id == profile.Id);

            _logger.LogInformation("Teacher profile {Id} updated for user {UserId}", profile!.Id, userId);
            return (true, "Profile saved", profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save teacher profile for user {UserId}", userId);
            return (false, "Failed to save profile", null);
        }
    }

    public async Task<(bool Success, string Message)> SetAvailabilityAsync(
        int teacherProfileId, List<AvailabilitySlotInput> slots)
    {
        try
        {
            var profile = await _context.TeacherProfiles.FindAsync(teacherProfileId);
            if (profile == null) return (false, "Teacher profile not found");

            var existing = await _context.TeacherAvailabilities
                .Where(ta => ta.TeacherProfileId == teacherProfileId)
                .ToListAsync();

            _context.TeacherAvailabilities.RemoveRange(existing);

            foreach (var slot in slots)
            {
                if (!TimeOnly.TryParse(slot.StartTime, out var start) ||
                    !TimeOnly.TryParse(slot.EndTime, out var end))
                {
                    continue;
                }

                if (start >= end) continue;

                _context.TeacherAvailabilities.Add(new TeacherAvailability
                {
                    TeacherProfileId = teacherProfileId,
                    DayOfWeek = slot.DayOfWeek,
                    StartTime = start,
                    EndTime = end
                });
            }

            await _context.SaveChangesAsync();
            return (true, "Availability updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set availability for teacher {Id}", teacherProfileId);
            return (false, "Failed to update availability");
        }
    }

    public async Task<(bool Success, string Message)> ToggleAcceptingStudentsAsync(int teacherProfileId, bool accepting)
    {
        var profile = await _context.TeacherProfiles.FindAsync(teacherProfileId);
        if (profile == null) return (false, "Teacher profile not found");

        profile.IsAcceptingStudents = accepting;
        await _context.SaveChangesAsync();
        return (true, accepting ? "Now accepting students" : "No longer accepting students");
    }

    // ─── Sessions ────────────────────────────────────────────

    public async Task<(bool Success, string Message, TutoringSession? Session)> CreateSessionAsync(
        int studentId, int teacherProfileId, int? subjectId, DateTime scheduledAt, int durationMinutes, string? notes)
    {
        try
        {
            var profile = await _context.TeacherProfiles.FindAsync(teacherProfileId);
            if (profile == null) return (false, "Teacher not found", null);
            if (!profile.IsAcceptingStudents) return (false, "Teacher is not accepting students", null);
            if (profile.UserId == studentId) return (false, "Cannot book yourself", null);

            if (scheduledAt <= DateTime.Now)
                return (false, "Session must be in the future", null);

            if (durationMinutes < 15 || durationMinutes > 240)
                return (false, "Duration must be 15-240 minutes", null);

            // Check teacher availability for the day of week
            var dayOfWeek = (int)scheduledAt.DayOfWeek;
            var sessionTime = TimeOnly.FromTimeSpan(scheduledAt.TimeOfDay);
            var sessionEndTime = sessionTime.AddMinutes(durationMinutes);

            var available = await _context.TeacherAvailabilities
                .AnyAsync(ta =>
                    ta.TeacherProfileId == teacherProfileId &&
                    ta.DayOfWeek == dayOfWeek &&
                    ta.StartTime <= sessionTime &&
                    ta.EndTime >= sessionEndTime);

            if (!available)
                return (false, "Teacher is not available at this time", null);

            // Check for overlapping sessions
            var overlapping = await _context.TutoringSessions
                .AnyAsync(ts =>
                    ts.TeacherId == profile.UserId &&
                    ts.Status != SessionStatus.Cancelled &&
                    ts.Status != SessionStatus.Declined &&
                    ts.ScheduledAt < scheduledAt.AddMinutes(durationMinutes) &&
                    ts.ScheduledAt.AddMinutes(ts.DurationMinutes) > scheduledAt);

            if (overlapping)
                return (false, "Teacher already has a session at this time", null);

            var session = new TutoringSession
            {
                TeacherId = profile.UserId,
                StudentId = studentId,
                SubjectTagId = subjectId,
                ScheduledAt = scheduledAt,
                DurationMinutes = durationMinutes,
                StudentNotes = notes,
                Status = SessionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.TutoringSessions.Add(session);
            await _context.SaveChangesAsync();

            session = await _context.TutoringSessions
                .Include(s => s.Teacher)
                .Include(s => s.Student)
                .Include(s => s.SubjectTag)
                .FirstOrDefaultAsync(s => s.Id == session.Id);

            _logger.LogInformation("Session {Id} created: student {Student} -> teacher {Teacher}",
                session!.Id, studentId, profile.UserId);
            return (true, "Session booked", session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session for student {StudentId}", studentId);
            return (false, "Failed to create session", null);
        }
    }

    public async Task<(bool Success, string Message)> RespondToSessionAsync(
        int teacherId, int sessionId, bool accept, string? teacherNotes = null)
    {
        try
        {
            var session = await _context.TutoringSessions.FindAsync(sessionId);
            if (session == null) return (false, "Session not found");
            if (session.TeacherId != teacherId) return (false, "Not your session");
            if (session.Status != SessionStatus.Pending)
                return (false, "Session already responded to");

            session.Status = accept ? SessionStatus.Accepted : SessionStatus.Declined;
            if (teacherNotes != null) session.TeacherNotes = teacherNotes;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Session {Id} {Status} by teacher {TeacherId}",
                sessionId, accept ? "accepted" : "declined", teacherId);
            return (true, accept ? "Session accepted" : "Session declined");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to respond to session {SessionId}", sessionId);
            return (false, "Failed to respond");
        }
    }

    public async Task<(bool Success, string Message)> CancelSessionAsync(int userId, int sessionId, string? reason = null)
    {
        try
        {
            var session = await _context.TutoringSessions.FindAsync(sessionId);
            if (session == null) return (false, "Session not found");
            if (session.TeacherId != userId && session.StudentId != userId)
                return (false, "Not your session");
            if (session.Status == SessionStatus.Completed || session.Status == SessionStatus.Cancelled)
                return (false, "Cannot cancel this session");

            session.Status = SessionStatus.Cancelled;
            await _context.SaveChangesAsync();

            return (true, "Session cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel session {SessionId}", sessionId);
            return (false, "Failed to cancel session");
        }
    }

    public async Task<(bool Success, string Message)> CompleteSessionAsync(int teacherId, int sessionId, string? teacherNotes = null)
    {
        try
        {
            var session = await _context.TutoringSessions.FindAsync(sessionId);
            if (session == null) return (false, "Session not found");
            if (session.TeacherId != teacherId) return (false, "Not your session");
            if (session.Status != SessionStatus.InProgress && session.Status != SessionStatus.Accepted)
                return (false, "Session not in progress");

            session.Status = SessionStatus.Completed;
            session.EndedAt = DateTime.UtcNow;
            if (teacherNotes != null) session.TeacherNotes = teacherNotes;

            await _context.SaveChangesAsync();
            return (true, "Session completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete session {SessionId}", sessionId);
            return (false, "Failed to complete session");
        }
    }

    public async Task<(bool Success, string Message)> StartSessionAsync(int userId, int sessionId)
    {
        try
        {
            var session = await _context.TutoringSessions.FindAsync(sessionId);
            if (session == null) return (false, "Session not found");
            if (session.TeacherId != userId && session.StudentId != userId)
                return (false, "Not your session");
            if (session.Status != SessionStatus.Accepted)
                return (false, "Session not accepted yet");

            session.Status = SessionStatus.InProgress;
            session.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "Session started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start session {SessionId}", sessionId);
            return (false, "Failed to start session");
        }
    }

    public async Task<List<TutoringSession>> GetUserSessionsAsync(int userId, SessionStatus? status = null, bool asTeacher = true)
    {
        var q = _context.TutoringSessions
            .Include(s => s.Teacher)
            .Include(s => s.Student)
            .Include(s => s.SubjectTag)
            .Where(s => asTeacher ? s.TeacherId == userId : s.StudentId == userId)
            .AsQueryable();

        if (status.HasValue)
            q = q.Where(s => s.Status == status.Value);

        return await q.OrderByDescending(s => s.ScheduledAt).ToListAsync();
    }

    public async Task<TutoringSession?> GetSessionAsync(int sessionId)
    {
        return await _context.TutoringSessions
            .Include(s => s.Teacher)
            .Include(s => s.Student)
            .Include(s => s.SubjectTag)
            .Include(s => s.Reviews).ThenInclude(r => r.Reviewer)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    public async Task<List<TutoringSession>> GetUpcomingSessionsAsync(int userId)
    {
        return await _context.TutoringSessions
            .Include(s => s.Teacher)
            .Include(s => s.Student)
            .Include(s => s.SubjectTag)
            .Where(s => (s.TeacherId == userId || s.StudentId == userId) &&
                        (s.Status == SessionStatus.Pending || s.Status == SessionStatus.Accepted) &&
                        s.ScheduledAt > DateTime.UtcNow)
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync();
    }

    // ─── Reviews ─────────────────────────────────────────────

    public async Task<(bool Success, string Message)> CreateReviewAsync(
        int reviewerId, int sessionId, int rating, string? comment)
    {
        try
        {
            if (rating < 1 || rating > 5)
                return (false, "Rating must be 1-5");

            var session = await _context.TutoringSessions.FindAsync(sessionId);
            if (session == null) return (false, "Session not found");
            if (session.Status != SessionStatus.Completed)
                return (false, "Can only review completed sessions");

            // Only student or teacher can review
            if (session.StudentId != reviewerId && session.TeacherId != reviewerId)
                return (false, "Not authorized to review this session");

            // Check if already reviewed
            var existing = await _context.TutoringReviews
                .AnyAsync(r => r.SessionId == sessionId && r.ReviewerId == reviewerId);
            if (existing)
                return (false, "You already reviewed this session");

            var review = new TutoringReview
            {
                SessionId = sessionId,
                ReviewerId = reviewerId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.TutoringReviews.Add(review);
            await _context.SaveChangesAsync();

            return (true, "Review submitted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create review for session {SessionId}", sessionId);
            return (false, "Failed to submit review");
        }
    }

    public async Task<List<TutoringReview>> GetTeacherReviewsAsync(int teacherProfileId, int limit = 10)
    {
        return await _context.TutoringReviews
            .Include(r => r.Reviewer)
            .Include(r => r.Session).ThenInclude(s => s.SubjectTag)
            .Where(r => r.Session.TeacherId ==
                _context.TeacherProfiles.Where(tp => tp.Id == teacherProfileId).Select(tp => tp.UserId).FirstOrDefault())
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<double?> GetTeacherAverageRatingAsync(int teacherProfileId)
    {
        var teacherUserId = await _context.TeacherProfiles
            .Where(tp => tp.Id == teacherProfileId)
            .Select(tp => tp.UserId)
            .FirstOrDefaultAsync();

        return await _context.TutoringReviews
            .Where(r => r.Session.TeacherId == teacherUserId)
            .AverageAsync(r => (double?)r.Rating);
    }
}

public class AvailabilitySlotInput
{
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}
