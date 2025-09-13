using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Data
{
    public class JournalDbContext : DbContext
    {
        public JournalDbContext(DbContextOptions<JournalDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserAssignment> UserAssignments { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ContentItem> Contents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Role entity
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Gender).HasMaxLength(20);
                entity.Property(e => e.Specialization).HasMaxLength(100);
                entity.Property(e => e.LicenseNumber).HasMaxLength(50);

                // Foreign key relationship to Role
                entity.HasOne(e => e.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure UserAssignment junction table (User-to-User relationships)
            modelBuilder.Entity<UserAssignment>(entity =>
            {
                entity.HasKey(e => new { e.AssignerId, e.AssigneeId });

                entity.HasOne(e => e.Assigner)
                      .WithMany(u => u.Assignments)
                      .HasForeignKey(e => e.AssignerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Assignee)
                      .WithMany(u => u.AssignedTo)
                      .HasForeignKey(e => e.AssigneeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure JournalEntry entity
            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired();
                entity.Property(e => e.Mood).HasMaxLength(50);

                // Foreign key relationship
                entity.HasOne(e => e.User)
                      .WithMany(u => u.JournalEntries)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ChatSession entity
            modelBuilder.Entity<ChatSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.SessionId).IsUnique();

                // Foreign key relationship
                entity.HasOne(e => e.User)
                      .WithMany(u => u.ChatSessions)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ContentItem entity
            modelBuilder.Entity<ContentItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContentGuid).IsRequired();
                entity.HasIndex(e => e.ContentGuid).IsUnique();
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.S3Bucket).IsRequired().HasMaxLength(100);
                entity.Property(e => e.S3Key).IsRequired().HasMaxLength(500);
                entity.Property(e => e.S3Url).HasMaxLength(1000);

                // Foreign key relationships
                entity.HasOne(e => e.Patient)
                      .WithMany()
                      .HasForeignKey(e => e.PatientId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AddedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.AddedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
