using Microsoft.EntityFrameworkCore;
using VideoChatingApp.WebRTC.Core.Models;

namespace VideoChatingApp.WebRTC.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Friendship> Friendships { get; set; } = null!;
    public DbSet<FriendshipRequest> FriendshipRequests { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<PostReaction> PostReactions { get; set; } = null!;
    public DbSet<PostComment> PostComments { get; set; } = null!;
    public DbSet<PushSubscription> PushSubscriptions { get; set; } = null!;
    public DbSet<SubjectTag> SubjectTags { get; set; } = null!;
    public DbSet<TeacherProfile> TeacherProfiles { get; set; } = null!;
    public DbSet<TeacherSubject> TeacherSubjects { get; set; } = null!;
    public DbSet<TeacherAvailability> TeacherAvailabilities { get; set; } = null!;
    public DbSet<TutoringSession> TutoringSessions { get; set; } = null!;
    public DbSet<TutoringReview> TutoringReviews { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(u => u.DisplayName)
                .HasMaxLength(100);

            entity.Property(u => u.ProfilePictureUrl)
                .HasMaxLength(500);

            entity.Property(u => u.Bio)
                .HasMaxLength(500);

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            // Add unique constraints
            entity.HasIndex(u => u.Username)
                .IsUnique();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            // Add indexes for better query performance
            entity.HasIndex(u => u.IsOnline);
        });

        // Configure Friendship entity
        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.HasKey(f => f.Id);

            entity.Property(f => f.CreatedAt)
                .IsRequired();

            // Configure User1 relationship
            entity.HasOne(f => f.User1)
                .WithMany(u => u.FriendshipsAsUser1)
                .HasForeignKey(f => f.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure User2 relationship
            entity.HasOne(f => f.User2)
                .WithMany(u => u.FriendshipsAsUser2)
                .HasForeignKey(f => f.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Add unique constraint to prevent duplicate friendships
            entity.HasIndex(f => new { f.User1Id, f.User2Id })
                .IsUnique();

            // Add index for better query performance
            entity.HasIndex(f => f.User1Id);
            entity.HasIndex(f => f.User2Id);
        });

        // Configure FriendshipRequest entity
        modelBuilder.Entity<FriendshipRequest>(entity =>
        {
            entity.HasKey(fr => fr.Id);

            entity.Property(fr => fr.Status)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(fr => fr.CreatedAt)
                .IsRequired();

            entity.Property(fr => fr.Message)
                .HasMaxLength(500);

            // Configure Sender relationship
            entity.HasOne(fr => fr.Sender)
                .WithMany(u => u.SentRequests)
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Receiver relationship
            entity.HasOne(fr => fr.Receiver)
                .WithMany(u => u.ReceivedRequests)
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add indexes for better query performance
            entity.HasIndex(fr => fr.SenderId);
            entity.HasIndex(fr => fr.ReceiverId);
            entity.HasIndex(fr => fr.Status);
            entity.HasIndex(fr => new { fr.SenderId, fr.ReceiverId, fr.Status });
        });

        // Configure ChatMessage entity
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(cm => cm.Id);

            entity.Property(cm => cm.Content)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(cm => cm.AttachmentUrl)
                .HasMaxLength(500);

            entity.Property(cm => cm.AttachmentType)
                .HasMaxLength(50);

            entity.Property(cm => cm.SentAt)
                .IsRequired();

            // Configure Sender relationship
            entity.HasOne(cm => cm.Sender)
                .WithMany()
                .HasForeignKey(cm => cm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Receiver relationship
            entity.HasOne(cm => cm.Receiver)
                .WithMany()
                .HasForeignKey(cm => cm.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add indexes for better query performance
            entity.HasIndex(cm => cm.SenderId);
            entity.HasIndex(cm => cm.ReceiverId);
            entity.HasIndex(cm => cm.SentAt);
            entity.HasIndex(cm => new { cm.SenderId, cm.ReceiverId, cm.SentAt });
            entity.HasIndex(cm => cm.IsRead);
        });

        // Configure Post entity
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Content)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(p => p.ImageUrl)
                .HasMaxLength(500);

            entity.HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.AuthorId);
            entity.HasIndex(p => p.CreatedAt);
            entity.HasIndex(p => p.IsDeleted);
        });

        // Configure PostReaction entity
        modelBuilder.Entity<PostReaction>(entity =>
        {
            entity.HasKey(pr => pr.Id);

            entity.Property(pr => pr.Type)
                .IsRequired()
                .HasConversion<string>();

            entity.HasOne(pr => pr.Post)
                .WithMany(p => p.Reactions)
                .HasForeignKey(pr => pr.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pr => pr.User)
                .WithMany(u => u.PostReactions)
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One reaction per user per post
            entity.HasIndex(pr => new { pr.PostId, pr.UserId }).IsUnique();
            entity.HasIndex(pr => pr.PostId);
            entity.HasIndex(pr => pr.UserId);
        });

        // Configure PostComment entity
        modelBuilder.Entity<PostComment>(entity =>
        {
            entity.HasKey(pc => pc.Id);

            entity.Property(pc => pc.Content)
                .IsRequired()
                .HasMaxLength(1000);

            entity.HasOne(pc => pc.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(pc => pc.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pc => pc.Author)
                .WithMany(u => u.PostComments)
                .HasForeignKey(pc => pc.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(pc => pc.PostId);
            entity.HasIndex(pc => pc.AuthorId);
            entity.HasIndex(pc => pc.CreatedAt);
        });

        // Configure Conversation entity
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.CreatedAt)
                .IsRequired();

            entity.Property(c => c.LastMessageAt)
                .IsRequired();

            // Configure User1 relationship
            entity.HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure User2 relationship
            entity.HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Add unique constraint to prevent duplicate conversations
            entity.HasIndex(c => new { c.User1Id, c.User2Id })
                .IsUnique();

            // Add index for better query performance
            entity.HasIndex(f => f.LastMessageAt);
        });

        // Configure SubjectTag
        modelBuilder.Entity<SubjectTag>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(s => s.Name).IsUnique();
        });

        // Configure TeacherProfile
        modelBuilder.Entity<TeacherProfile>(entity =>
        {
            entity.HasKey(tp => tp.Id);
            entity.HasOne(tp => tp.User).WithMany().HasForeignKey(tp => tp.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(tp => tp.UserId).IsUnique();
            entity.HasIndex(tp => tp.IsAcceptingStudents);
        });

        // Configure TeacherSubject
        modelBuilder.Entity<TeacherSubject>(entity =>
        {
            entity.HasKey(ts => ts.Id);
            entity.HasOne(ts => ts.TeacherProfile).WithMany(tp => tp.Subjects).HasForeignKey(ts => ts.TeacherProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ts => ts.SubjectTag).WithMany().HasForeignKey(ts => ts.SubjectTagId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(ts => new { ts.TeacherProfileId, ts.SubjectTagId }).IsUnique();
        });

        // Configure TeacherAvailability
        modelBuilder.Entity<TeacherAvailability>(entity =>
        {
            entity.HasKey(ta => ta.Id);
            entity.HasOne(ta => ta.TeacherProfile).WithMany(tp => tp.AvailabilitySlots).HasForeignKey(ta => ta.TeacherProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(ta => new { ta.TeacherProfileId, ta.DayOfWeek });
        });

        // Configure TutoringSession
        modelBuilder.Entity<TutoringSession>(entity =>
        {
            entity.HasKey(ts => ts.Id);
            entity.HasOne(ts => ts.Teacher).WithMany().HasForeignKey(ts => ts.TeacherId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(ts => ts.Student).WithMany().HasForeignKey(ts => ts.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(ts => ts.SubjectTag).WithMany().HasForeignKey(ts => ts.SubjectTagId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(ts => ts.TeacherId);
            entity.HasIndex(ts => ts.StudentId);
            entity.HasIndex(ts => ts.Status);
            entity.HasIndex(ts => ts.ScheduledAt);
        });

        // Configure TutoringReview
        modelBuilder.Entity<TutoringReview>(entity =>
        {
            entity.HasKey(tr => tr.Id);
            entity.HasOne(tr => tr.Session).WithMany(s => s.Reviews).HasForeignKey(tr => tr.SessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(tr => tr.Reviewer).WithMany().HasForeignKey(tr => tr.ReviewerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(tr => tr.SessionId).IsUnique();
            entity.HasIndex(tr => tr.Rating);
        });

        // Seed SubjectTags
        modelBuilder.Entity<SubjectTag>().HasData(
            new SubjectTag { Id = 1, Name = "Mathematics", Description = "Algebra, Calculus, Statistics, Geometry", Icon = "📐" },
            new SubjectTag { Id = 2, Name = "Physics", Description = "Mechanics, Thermodynamics, Electromagnetism", Icon = "⚛️" },
            new SubjectTag { Id = 3, Name = "Chemistry", Description = "Organic, Inorganic, Physical Chemistry", Icon = "🧪" },
            new SubjectTag { Id = 4, Name = "Biology", Description = "Cell Biology, Genetics, Ecology", Icon = "🧬" },
            new SubjectTag { Id = 5, Name = "Computer Science", Description = "Programming, Algorithms, Data Structures", Icon = "💻" },
            new SubjectTag { Id = 6, Name = "English", Description = "Grammar, Writing, Literature", Icon = "📝" },
            new SubjectTag { Id = 7, Name = "Spanish", Description = "Conversational, Grammar, Writing", Icon = "🗣️" },
            new SubjectTag { Id = 8, Name = "French", Description = "Conversational, Grammar, Writing", Icon = "🥐" },
            new SubjectTag { Id = 9, Name = "History", Description = "World History, US History, European History", Icon = "📜" },
            new SubjectTag { Id = 10, Name = "Economics", Description = "Micro, Macroeconomics, Finance", Icon = "📊" },
            new SubjectTag { Id = 11, Name = "Music", Description = "Piano, Guitar, Theory, Vocal", Icon = "🎵" },
            new SubjectTag { Id = 12, Name = "Art", Description = "Drawing, Painting, Digital Art", Icon = "🎨" }
        );
    }
}
