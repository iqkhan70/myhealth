using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Server.Models;

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
            public DbSet<ChatMessage> ChatMessages { get; set; }
            public DbSet<ContentItem> Contents { get; set; }
            public DbSet<ContentAnalysis> ContentAnalyses { get; set; }
            public DbSet<ContentAlert> ContentAlerts { get; set; }

            // Emergency system entities
            public DbSet<RegisteredDevice> RegisteredDevices { get; set; }
            public DbSet<EmergencyIncident> EmergencyIncidents { get; set; }

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
                        entity.Property(e => e.MobilePhone).HasMaxLength(20);
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
                        entity.Property(e => e.Summary).HasMaxLength(2000);
                        entity.Property(e => e.PrivacyLevel).HasConversion<string>();

                        // Foreign key relationships
                        entity.HasOne(e => e.User)
                        .WithMany(u => u.ChatSessions)
                        .HasForeignKey(e => e.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.Patient)
                        .WithMany()
                        .HasForeignKey(e => e.PatientId)
                        .OnDelete(DeleteBehavior.SetNull);
                  });

                  // Configure ChatMessage entity
                  modelBuilder.Entity<ChatMessage>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Content).IsRequired().HasColumnType("TEXT");
                        entity.Property(e => e.Metadata).HasMaxLength(1000);
                        entity.Property(e => e.Role).HasConversion<string>();
                        entity.Property(e => e.MessageType).HasConversion<string>();

                        // Foreign key relationship
                        entity.HasOne(e => e.Session)
                        .WithMany(s => s.Messages)
                        .HasForeignKey(e => e.SessionId)
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
                        // entity.Property(e => e.S3Url).HasMaxLength(1000); // Removed - URLs generated on-demand

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

                  // Configure ContentAnalysis entity
                  modelBuilder.Entity<ContentAnalysis>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.ContentType).IsRequired().HasMaxLength(50);
                        entity.Property(e => e.ExtractedText).HasColumnType("TEXT");
                        entity.Property(e => e.ProcessingStatus).IsRequired().HasMaxLength(50);
                        entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

                        // Configure Dictionary as JSON
                        entity.Property(e => e.AnalysisResults)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new Dictionary<string, object>())
                        .HasColumnType("JSON");

                        // Foreign key relationship
                        entity.HasOne(e => e.Content)
                        .WithMany()
                        .HasForeignKey(e => e.ContentId)
                        .OnDelete(DeleteBehavior.Cascade);

                        // Add unique constraint to prevent duplicate analyses for the same content
                        entity.HasIndex(e => e.ContentId)
                        .IsUnique();
                  });

                  // Configure ContentAlert entity
                  modelBuilder.Entity<ContentAlert>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.AlertType).IsRequired().HasMaxLength(50);
                        entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                        entity.Property(e => e.Description).HasMaxLength(1000);
                        entity.Property(e => e.Severity).IsRequired().HasMaxLength(20);

                        // Foreign key relationships
                        entity.HasOne(e => e.Content)
                        .WithMany()
                        .HasForeignKey(e => e.ContentId)
                        .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.Patient)
                        .WithMany()
                        .HasForeignKey(e => e.PatientId)
                        .OnDelete(DeleteBehavior.Cascade);
                  });

                  // Configure RegisteredDevice entity
                  modelBuilder.Entity<RegisteredDevice>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.DeviceName).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.DeviceType).IsRequired().HasMaxLength(50);
                        entity.Property(e => e.DeviceModel).HasMaxLength(100);
                        entity.Property(e => e.OperatingSystem).HasMaxLength(50);
                        entity.Property(e => e.DeviceToken).IsRequired().HasMaxLength(500);
                        entity.Property(e => e.PublicKey).IsRequired().HasMaxLength(2000);
                        entity.Property(e => e.LastKnownLocation).HasMaxLength(500);

                        entity.HasIndex(e => e.DeviceId).IsUnique();
                        entity.HasIndex(e => e.DeviceToken).IsUnique();

                        // Foreign key relationship to User
                        entity.HasOne(e => e.Patient)
                        .WithMany()
                        .HasForeignKey(e => e.PatientId)
                        .OnDelete(DeleteBehavior.Cascade);
                  });

                  // Configure EmergencyIncident entity
                  modelBuilder.Entity<EmergencyIncident>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.EmergencyType).IsRequired().HasMaxLength(50);
                        entity.Property(e => e.Severity).IsRequired().HasMaxLength(20);
                        entity.Property(e => e.Message).HasMaxLength(1000);
                        entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.DeviceToken).IsRequired().HasMaxLength(500);
                        entity.Property(e => e.DoctorResponse).HasMaxLength(1000);
                        entity.Property(e => e.ActionTaken).HasMaxLength(1000);
                        entity.Property(e => e.Resolution).HasMaxLength(1000);
                        entity.Property(e => e.VitalSignsJson).HasMaxLength(2000);
                        entity.Property(e => e.LocationJson).HasMaxLength(1000);
                        entity.Property(e => e.IpAddress).HasMaxLength(50);
                        entity.Property(e => e.UserAgent).HasMaxLength(500);

                        entity.HasIndex(e => e.PatientId);
                        entity.HasIndex(e => e.DoctorId);
                        entity.HasIndex(e => e.Timestamp);
                        entity.HasIndex(e => e.DeviceToken);

                        // Foreign key relationships
                        entity.HasOne(e => e.Patient)
                        .WithMany()
                        .HasForeignKey(e => e.PatientId)
                        .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.Doctor)
                        .WithMany()
                        .HasForeignKey(e => e.DoctorId)
                        .OnDelete(DeleteBehavior.Restrict);
                  });
            }
      }
}
