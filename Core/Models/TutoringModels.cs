using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VideoChatingApp.WebRTC.Core.Models;

public class SubjectTag
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? Icon { get; set; }
}

public class TeacherProfile
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(2000)]
    public string? HourlyRate { get; set; }

    public int ExperienceYears { get; set; }

    public bool IsAcceptingStudents { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<TeacherSubject> Subjects { get; set; } = new List<TeacherSubject>();
    public ICollection<TeacherAvailability> AvailabilitySlots { get; set; } = new List<TeacherAvailability>();
}

public class TeacherSubject
{
    [Key]
    public int Id { get; set; }

    public int TeacherProfileId { get; set; }
    public TeacherProfile TeacherProfile { get; set; } = null!;

    public int SubjectTagId { get; set; }
    public SubjectTag SubjectTag { get; set; } = null!;
}

public class TeacherAvailability
{
    [Key]
    public int Id { get; set; }

    public int TeacherProfileId { get; set; }
    public TeacherProfile TeacherProfile { get; set; } = null!;

    // Day of week (0=Sunday .. 6=Saturday)
    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SessionStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Cancelled = 3,
    Completed = 4,
    InProgress = 5,
    Ready = 6
}

public class TutoringSession
{
    [Key]
    public int Id { get; set; }

    public int TeacherId { get; set; }
    public User Teacher { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public int? SubjectTagId { get; set; }
    public SubjectTag? SubjectTag { get; set; }

    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;

    public SessionStatus Status { get; set; } = SessionStatus.Pending;

    [MaxLength(2000)]
    public string? StudentNotes { get; set; }

    [MaxLength(2000)]
    public string? TeacherNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    // Set true once the start-time reminder/nudge has been sent
    public bool ReminderSent { get; set; }

    // Navigation
    public ICollection<TutoringReview> Reviews { get; set; } = new List<TutoringReview>();
}

public class TutoringReview
{
    [Key]
    public int Id { get; set; }

    public int SessionId { get; set; }
    public TutoringSession Session { get; set; } = null!;

    public int ReviewerId { get; set; }
    public User Reviewer { get; set; } = null!;

    public int Rating { get; set; } // 1-5

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
