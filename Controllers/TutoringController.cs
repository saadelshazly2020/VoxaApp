using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoChatingApp.WebRTC.Core.Models;
using VideoChatingApp.WebRTC.Core.Services;

namespace VideoChatingApp.WebRTC.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TutoringController : ControllerBase
{
    private readonly ITutoringService _tutoring;
    private readonly ILogger<TutoringController> _logger;

    public TutoringController(ITutoringService tutoring, ILogger<TutoringController> logger)
    {
        _tutoring = tutoring;
        _logger = logger;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    // ─── Subjects ────────────────────────────────────────────

    [HttpGet("subjects")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSubjects()
    {
        var subjects = await _tutoring.GetSubjectsAsync();
        return Ok(subjects);
    }

    // ─── Teacher Directory ───────────────────────────────────

    [HttpGet("teachers")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchTeachers(
        [FromQuery] int? subjectId,
        [FromQuery] string? query)
    {
        var teachers = await _tutoring.SearchTeachersAsync(subjectId, query);
        var currentUserId = GetUserId();

        var results = teachers.Select(tp => new
        {
            tp.Id,
            tp.User.DisplayName,
            tp.User.Username,
            tp.User.ProfilePictureUrl,
            tp.User.IsOnline,
            tp.HourlyRate,
            tp.ExperienceYears,
            tp.IsAcceptingStudents,
            Subjects = tp.Subjects.Select(ts => new
            {
                ts.SubjectTag.Id,
                ts.SubjectTag.Name,
                ts.SubjectTag.Icon
            }),
            AverageRating = (double?)null // computed in controller to avoid extra query
        });

        return Ok(results);
    }

    [HttpGet("teachers/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTeacherProfile(int id)
    {
        var profile = await _tutoring.GetTeacherProfileByTeacherIdAsync(id);
        if (profile == null) return NotFound();

        var avgRating = await _tutoring.GetTeacherAverageRatingAsync(id);
        var reviews = await _tutoring.GetTeacherReviewsAsync(id, 5);

        return Ok(new
        {
            profile.Id,
            UserId = profile.User.Id,
            profile.User.DisplayName,
            profile.User.Username,
            profile.User.ProfilePictureUrl,
            profile.User.Bio,
            profile.User.IsOnline,
            profile.HourlyRate,
            profile.ExperienceYears,
            profile.IsAcceptingStudents,
            profile.CreatedAt,
            Subjects = profile.Subjects.Select(ts => new
            {
                ts.SubjectTag.Id,
                ts.SubjectTag.Name,
                ts.SubjectTag.Icon
            }),
            Availability = profile.AvailabilitySlots.Select(a => new
            {
                a.DayOfWeek,
                a.StartTime,
                a.EndTime
            }),
            AverageRating = avgRating,
            ReviewCount = reviews.Count,
            Reviews = reviews.Select(r => new
            {
                r.Id,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                ReviewerDisplayName = r.Reviewer.DisplayName,
                ReviewerProfilePictureUrl = r.Reviewer.ProfilePictureUrl,
                SubjectName = r.Session.SubjectTag?.Name
            })
        });
    }

    // ─── My Teacher Profile ──────────────────────────────────

    [HttpGet("my-teacher-profile")]
    public async Task<IActionResult> GetMyTeacherProfile()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var profile = await _tutoring.GetTeacherProfileAsync(userId);
        if (profile == null) return Ok(null);

        return Ok(new
        {
            profile.Id,
            profile.HourlyRate,
            profile.ExperienceYears,
            profile.IsAcceptingStudents,
            profile.CreatedAt,
            Subjects = profile.Subjects.Select(ts => new
            {
                ts.SubjectTag.Id,
                ts.SubjectTag.Name,
                ts.SubjectTag.Icon
            }),
            Availability = profile.AvailabilitySlots.Select(a => new
            {
                a.DayOfWeek,
                a.StartTime,
                a.EndTime
            })
        });
    }

    [HttpPost("my-teacher-profile")]
    public async Task<IActionResult> SaveMyTeacherProfile([FromBody] SaveTeacherProfileRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message, profile) = await _tutoring.CreateOrUpdateTeacherProfileAsync(
            userId, request.HourlyRate, request.ExperienceYears, request.SubjectIds);

        if (!success) return BadRequest(new { message });

        if (request.Availability != null && profile != null)
        {
            await _tutoring.SetAvailabilityAsync(profile.Id, request.Availability);
        }

        return Ok(new { message, profileId = profile?.Id });
    }

    [HttpPut("my-teacher-profile/availability")]
    public async Task<IActionResult> UpdateAvailability([FromBody] List<AvailabilitySlotInput> slots)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var profile = await _tutoring.GetTeacherProfileAsync(userId);
        if (profile == null) return NotFound(new { message = "No teacher profile" });

        var (success, message) = await _tutoring.SetAvailabilityAsync(profile.Id, slots);
        if (!success) return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPut("my-teacher-profile/toggle-accepting")]
    public async Task<IActionResult> ToggleAccepting([FromBody] ToggleAcceptingRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var profile = await _tutoring.GetTeacherProfileAsync(userId);
        if (profile == null) return NotFound(new { message = "No teacher profile" });

        var (success, message) = await _tutoring.ToggleAcceptingStudentsAsync(profile.Id, request.Accepting);
        if (!success) return BadRequest(new { message });

        return Ok(new { message });
    }

    // ─── Sessions ────────────────────────────────────────────

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message, session) = await _tutoring.CreateSessionAsync(
            userId, request.TeacherProfileId, request.SubjectId,
            request.ScheduledAt, request.DurationMinutes, request.Notes);

        if (!success) return BadRequest(new { message });

        return Ok(new
        {
            message,
            session = new
            {
                session!.Id,
                session.Status,
                TeacherDisplayName = session.Teacher.DisplayName,
                StudentDisplayName = session.Student.DisplayName,
                SubjectName = session.SubjectTag?.Name,
                session.ScheduledAt,
                session.DurationMinutes,
                session.CreatedAt
            }
        });
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetMySessions(
        [FromQuery] string? role,
        [FromQuery] string? status)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var asTeacher = role != "student";
        SessionStatus? statusEnum = status?.ToLower() switch
        {
            "pending" => SessionStatus.Pending,
            "accepted" => SessionStatus.Accepted,
            "declined" => SessionStatus.Declined,
            "cancelled" => SessionStatus.Cancelled,
            "completed" => SessionStatus.Completed,
            "inprogress" => SessionStatus.InProgress,
            _ => null
        };

        var sessions = await _tutoring.GetUserSessionsAsync(userId, statusEnum, asTeacher);

        return Ok(sessions.Select(s => new
        {
            s.Id,
            s.Status,
            TeacherDisplayName = s.Teacher.DisplayName,
            TeacherProfilePictureUrl = s.Teacher.ProfilePictureUrl,
            StudentDisplayName = s.Student.DisplayName,
            StudentProfilePictureUrl = s.Student.ProfilePictureUrl,
            SubjectName = s.SubjectTag?.Name,
            SubjectIcon = s.SubjectTag?.Icon,
            s.ScheduledAt,
            s.DurationMinutes,
            s.StudentNotes,
            s.TeacherNotes,
            s.CreatedAt,
            s.StartedAt,
            s.EndedAt
        }));
    }

    [HttpGet("sessions/upcoming")]
    public async Task<IActionResult> GetUpcomingSessions()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var sessions = await _tutoring.GetUpcomingSessionsAsync(userId);

        return Ok(sessions.Select(s => new
        {
            s.Id,
            s.Status,
            TeacherDisplayName = s.Teacher.DisplayName,
            TeacherId = s.TeacherId,
            StudentDisplayName = s.Student.DisplayName,
            StudentId = s.StudentId,
            SubjectName = s.SubjectTag?.Name,
            SubjectIcon = s.SubjectTag?.Icon,
            s.ScheduledAt,
            s.DurationMinutes
        }));
    }

    [HttpGet("sessions/{id}")]
    public async Task<IActionResult> GetSession(int id)
    {
        var session = await _tutoring.GetSessionAsync(id);
        if (session == null) return NotFound();

        var userId = GetUserId();
        if (session.TeacherId != userId && session.StudentId != userId)
            return Forbid();

        return Ok(new
        {
            session.Id,
            session.Status,
            Teacher = new
            {
                session.Teacher.Id,
                session.Teacher.DisplayName,
                session.Teacher.ProfilePictureUrl
            },
            Student = new
            {
                session.Student.Id,
                session.Student.DisplayName,
                session.Student.ProfilePictureUrl
            },
            Subject = session.SubjectTag != null ? new
            {
                session.SubjectTag.Id,
                session.SubjectTag.Name,
                session.SubjectTag.Icon
            } : null,
            session.ScheduledAt,
            session.DurationMinutes,
            session.StudentNotes,
            session.TeacherNotes,
            session.CreatedAt,
            session.StartedAt,
            session.EndedAt,
            Review = session.Reviews.FirstOrDefault() != null ? new
            {
                session.Reviews.First().Id,
                session.Reviews.First().Rating,
                session.Reviews.First().Comment,
                session.Reviews.First().CreatedAt,
                ReviewerDisplayName = session.Reviews.First().Reviewer.DisplayName
            } : null
        });
    }

    [HttpPost("sessions/{id}/accept")]
    public async Task<IActionResult> AcceptSession(int id, [FromBody] RespondToSessionRequest? request = null)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message) = await _tutoring.RespondToSessionAsync(userId, id, true, request?.Notes);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    [HttpPost("sessions/{id}/decline")]
    public async Task<IActionResult> DeclineSession(int id, [FromBody] RespondToSessionRequest? request = null)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message) = await _tutoring.RespondToSessionAsync(userId, id, false, request?.Notes);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    [HttpPost("sessions/{id}/cancel")]
    public async Task<IActionResult> CancelSession(int id)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message) = await _tutoring.CancelSessionAsync(userId, id);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    [HttpPost("sessions/{id}/start")]
    public async Task<IActionResult> StartSession(int id)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message) = await _tutoring.StartSessionAsync(userId, id);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    [HttpPost("sessions/{id}/complete")]
    public async Task<IActionResult> CompleteSession(int id, [FromBody] CompleteSessionRequest? request = null)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message) = await _tutoring.CompleteSessionAsync(userId, id, request?.TeacherNotes);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    // ─── Reviews ─────────────────────────────────────────────

    [HttpPost("sessions/{id}/review")]
    public async Task<IActionResult> CreateReview(int id, [FromBody] CreateReviewRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message) = await _tutoring.CreateReviewAsync(
            userId, id, request.Rating, request.Comment);

        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }
}

// ─── Request DTOs ───────────────────────────────────────────────

public class SaveTeacherProfileRequest
{
    public string? HourlyRate { get; set; }
    public int ExperienceYears { get; set; }
    public List<int> SubjectIds { get; set; } = new();
    public List<AvailabilitySlotInput>? Availability { get; set; }
}

public class ToggleAcceptingRequest
{
    public bool Accepting { get; set; }
}

public class CreateSessionRequest
{
    public int TeacherProfileId { get; set; }
    public int? SubjectId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? Notes { get; set; }
}

public class RespondToSessionRequest
{
    public string? Notes { get; set; }
}

public class CompleteSessionRequest
{
    public string? TeacherNotes { get; set; }
}

public class CreateReviewRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
