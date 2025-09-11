using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Data
{
    public class JournalDbContext : DbContext
    {
        public JournalDbContext(DbContextOptions<JournalDbContext> options) : base(options) { }
        
        public DbSet<Role> Roles { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<DoctorPatient> DoctorPatients { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }

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

            // Configure Patient entity
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Gender).HasMaxLength(20);
                
                // Foreign key relationship to Role
                entity.HasOne(e => e.Role)
                      .WithMany(r => r.Patients)
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Doctor entity
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Specialization).HasMaxLength(100);
                entity.Property(e => e.LicenseNumber).HasMaxLength(50);
                
                // Foreign key relationship to Role
                entity.HasOne(e => e.Role)
                      .WithMany(r => r.Doctors)
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure DoctorPatient junction table
            modelBuilder.Entity<DoctorPatient>(entity =>
            {
                entity.HasKey(e => new { e.DoctorId, e.PatientId });
                
                entity.HasOne(e => e.Doctor)
                      .WithMany(d => d.DoctorPatients)
                      .HasForeignKey(e => e.DoctorId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.Patient)
                      .WithMany(p => p.DoctorPatients)
                      .HasForeignKey(e => e.PatientId)
                      .OnDelete(DeleteBehavior.Cascade);
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
