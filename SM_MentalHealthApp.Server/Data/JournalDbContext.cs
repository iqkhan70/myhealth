using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Data
{
    public class JournalDbContext : DbContext
    {
        public JournalDbContext(DbContextOptions<JournalDbContext> options) : base(options) { }
        
        public DbSet<Patient> Patients { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Patient entity
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Gender).HasMaxLength(20);
            });

            // Configure JournalEntry entity
            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired();
                entity.Property(e => e.Mood).HasMaxLength(50);
                
                // Foreign key relationship
                entity.HasOne(e => e.Patient)
                      .WithMany(p => p.JournalEntries)
                      .HasForeignKey(e => e.PatientId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ChatSession entity
            modelBuilder.Entity<ChatSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.SessionId).IsUnique();
                
                // Foreign key relationship
                entity.HasOne(e => e.Patient)
                      .WithMany(p => p.ChatSessions)
                      .HasForeignKey(e => e.PatientId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
