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
            entity.HasIndex(c => c.LastMessageAt);
        });
    }
}
