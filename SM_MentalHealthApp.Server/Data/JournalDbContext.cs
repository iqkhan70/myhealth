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
            public DbSet<UserRequest> UserRequests { get; set; }
            public DbSet<ServiceRequest> ServiceRequests { get; set; }
            public DbSet<ServiceRequestAssignment> ServiceRequestAssignments { get; set; }
            public DbSet<ServiceRequestCharge> ServiceRequestCharges { get; set; }
            public DbSet<Company> Companies { get; set; }
            public DbSet<BillingAccount> BillingAccounts { get; set; }
            public DbSet<BillingRate> BillingRates { get; set; }
            public DbSet<Expertise> Expertises { get; set; }
            public DbSet<SmeExpertise> SmeExpertises { get; set; }
            public DbSet<ServiceRequestExpertise> ServiceRequestExpertises { get; set; }
            public DbSet<ZipCodeLookup> ZipCodeLookups { get; set; }
            public DbSet<SmeInvoice> SmeInvoices { get; set; }
            public DbSet<SmeInvoiceLine> SmeInvoiceLines { get; set; }
            public DbSet<JournalEntry> JournalEntries { get; set; }
            public DbSet<ChatSession> ChatSessions { get; set; }
            public DbSet<ChatMessage> ChatMessages { get; set; }
            public DbSet<SmsMessage> SmsMessages { get; set; }
            public DbSet<ContentItem> Contents { get; set; }
            public DbSet<ContentTypeModel> ContentTypes { get; set; }
            public DbSet<ContentAnalysis> ContentAnalyses { get; set; }
            public DbSet<ContentAlert> ContentAlerts { get; set; }
            public DbSet<ClinicalNote> ClinicalNotes { get; set; }

            // Emergency system entities
            public DbSet<RegisteredDevice> RegisteredDevices { get; set; }
            public DbSet<EmergencyIncident> EmergencyIncidents { get; set; }

            // Appointment system entities
            public DbSet<Appointment> Appointments { get; set; }
            public DbSet<DoctorAvailability> DoctorAvailabilities { get; set; }

            // Critical value pattern system entities
            public DbSet<CriticalValueCategory> CriticalValueCategories { get; set; }
            public DbSet<CriticalValuePattern> CriticalValuePatterns { get; set; }
            public DbSet<CriticalValueKeyword> CriticalValueKeywords { get; set; }
            public DbSet<AIInstructionCategory> AIInstructionCategories { get; set; }
            public DbSet<AIInstruction> AIInstructions { get; set; }

            // Generic question pattern system entities
            public DbSet<GenericQuestionPattern> GenericQuestionPatterns { get; set; }

            // Knowledge base system entities
            public DbSet<KnowledgeBaseCategory> KnowledgeBaseCategories { get; set; }
            public DbSet<KnowledgeBaseEntry> KnowledgeBaseEntries { get; set; }

            // AI Response Template system entities
            public DbSet<AIResponseTemplate> AIResponseTemplates { get; set; }

            // Medical Threshold system entities
            public DbSet<MedicalThreshold> MedicalThresholds { get; set; }

            // Section Marker system entities
            public DbSet<SectionMarker> SectionMarkers { get; set; }

            // AI Model Configuration system entities
            public DbSet<AIModelConfig> AIModelConfigs { get; set; }
            public DbSet<AIModelChain> AIModelChains { get; set; }

            // Lookup tables for Lead Intake
            public DbSet<State> States { get; set; }
            public DbSet<AccidentParticipantRole> AccidentParticipantRoles { get; set; }
            public DbSet<VehicleDisposition> VehicleDispositions { get; set; }
            public DbSet<TransportToCareMethod> TransportToCareMethods { get; set; }
            public DbSet<MedicalAttentionType> MedicalAttentionTypes { get; set; }
            public DbSet<SymptomOngoingStatus> SymptomOngoingStatuses { get; set; }

            // Client Profile System for Agentic AI
            public DbSet<ClientProfile> ClientProfiles { get; set; }
            public DbSet<ClientInteractionPattern> ClientInteractionPatterns { get; set; }
            public DbSet<ClientKeywordReaction> ClientKeywordReactions { get; set; }
            public DbSet<ClientServicePreference> ClientServicePreferences { get; set; }
            public DbSet<ClientInteractionHistory> ClientInteractionHistories { get; set; }

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

                        // MobilePhone is stored encrypted as string
                        entity.Property(e => e.MobilePhoneEncrypted)
                            .HasMaxLength(500); // Enough for encrypted phone number

                        // Ignore the computed MobilePhone property (not stored in DB)
                        entity.Ignore(e => e.MobilePhone);

                        entity.Property(e => e.Specialization).HasMaxLength(100);
                        entity.Property(e => e.LicenseNumber).HasMaxLength(50);

                        // Accident-related fields (captured from user requests)
                        entity.Property(e => e.Age);
                        entity.Property(e => e.Race).HasMaxLength(100);
                        entity.Property(e => e.AccidentAddress).HasColumnType("text");
                        entity.Property(e => e.AccidentDate);
                        entity.Property(e => e.VehicleDetails).HasColumnType("text");
                        entity.Property(e => e.DateReported);
                        entity.Property(e => e.PoliceCaseNumber).HasMaxLength(100);
                        entity.Property(e => e.AccidentDetails).HasColumnType("text");
                        entity.Property(e => e.RoadConditions).HasMaxLength(200);
                        entity.Property(e => e.DoctorsInformation).HasColumnType("text");
                        entity.Property(e => e.LawyersInformation).HasColumnType("text");
                        entity.Property(e => e.AdditionalNotes).HasColumnType("text");

                        // DateOfBirth is stored encrypted as string
                        entity.Property(e => e.DateOfBirthEncrypted)
                            .IsRequired()
                            .HasMaxLength(500); // Enough for encrypted DateTime

                        // Ignore the computed DateOfBirth property (not stored in DB)
                        entity.Ignore(e => e.DateOfBirth);

                        // Foreign key relationship to Role
                        entity.HasOne(e => e.Role)
                        .WithMany(r => r.Users)
                        .HasForeignKey(e => e.RoleId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Foreign key relationship to Company
                        entity.HasOne(e => e.Company)
                        .WithMany(c => c.Users)
                        .HasForeignKey(e => e.CompanyId)
                        .OnDelete(DeleteBehavior.SetNull);

                        // Performance indexes for common queries
                        entity.HasIndex(e => e.RoleId); // Critical for filtering patients (RoleId = 1)
                        entity.HasIndex(e => e.IsActive); // Critical for filtering active users
                        entity.HasIndex(e => new { e.RoleId, e.IsActive }); // Composite index for common filter combination
                        entity.HasIndex(e => e.FirstName); // For name searches
                        entity.HasIndex(e => e.LastName); // For name searches
                        entity.HasIndex(e => e.CompanyId); // For company-based queries

                        // Lead Intake fields
                        entity.Property(e => e.ResidenceStateCode).HasMaxLength(2);
                        entity.Property(e => e.AccidentStateCode).HasMaxLength(2);
                        entity.Property(e => e.SymptomsNotes).HasColumnType("text");

                        // Note: Foreign key relationships are managed at the database level via SQL script
                        // We don't configure them in EF Core to avoid validation issues if tables don't exist yet
                        // Indexes for Lead Intake fields (for performance)
                        entity.HasIndex(e => e.ResidenceStateCode);
                        entity.HasIndex(e => e.AccidentStateCode);
                        entity.HasIndex(e => e.AccidentParticipantRoleId);
                        entity.HasIndex(e => e.VehicleDispositionId);
                        entity.HasIndex(e => e.TransportToCareMethodId);
                        entity.HasIndex(e => e.MedicalAttentionTypeId);

                        // Password reset fields
                        entity.Property(e => e.PasswordResetToken).HasMaxLength(500);
                        entity.HasIndex(e => e.PasswordResetToken); // Index for faster token lookups
                  });

                  // Configure State entity
                  modelBuilder.Entity<State>(entity =>
                  {
                        entity.ToTable("States"); // Explicit table name
                        entity.HasKey(e => e.Code);
                        entity.Property(e => e.Code).IsRequired().HasMaxLength(2);
                        entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                        entity.HasIndex(e => e.Name).IsUnique();
                  });

                  // Configure AccidentParticipantRole entity
                  modelBuilder.Entity<AccidentParticipantRole>(entity =>
                  {
                        entity.ToTable("AccidentParticipantRole"); // Explicit table name (singular, matches SQL script)
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
                        entity.Property(e => e.Label).IsRequired().HasMaxLength(50);
                        entity.HasIndex(e => e.Code).IsUnique();
                  });

                  // Configure VehicleDisposition entity
                  modelBuilder.Entity<VehicleDisposition>(entity =>
                  {
                        entity.ToTable("VehicleDisposition"); // Explicit table name (singular, matches SQL script)
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
                        entity.Property(e => e.Label).IsRequired().HasMaxLength(50);
                        entity.HasIndex(e => e.Code).IsUnique();
                  });

                  // Configure TransportToCareMethod entity
                  modelBuilder.Entity<TransportToCareMethod>(entity =>
                  {
                        entity.ToTable("TransportToCareMethod"); // Explicit table name (singular, matches SQL script)
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
                        entity.Property(e => e.Label).IsRequired().HasMaxLength(80);
                        entity.HasIndex(e => e.Code).IsUnique();
                  });

                  // Configure MedicalAttentionType entity
                  modelBuilder.Entity<MedicalAttentionType>(entity =>
                  {
                        entity.ToTable("MedicalAttentionType"); // Explicit table name (singular, matches SQL script)
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
                        entity.Property(e => e.Label).IsRequired().HasMaxLength(80);
                        entity.HasIndex(e => e.Code).IsUnique();
                  });

                  // Configure SymptomOngoingStatus entity
                  modelBuilder.Entity<SymptomOngoingStatus>(entity =>
                  {
                        entity.ToTable("SymptomOngoingStatus"); // Explicit table name (singular, matches SQL script)
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
                        entity.Property(e => e.Label).IsRequired().HasMaxLength(80);
                        entity.HasIndex(e => e.Code).IsUnique();
                  });

                  // Configure UserRequest entity
                  modelBuilder.Entity<UserRequest>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                        entity.HasIndex(e => e.Email);

                        // MobilePhone is stored encrypted as string
                        entity.Property(e => e.MobilePhoneEncrypted)
                            .IsRequired()
                            .HasMaxLength(500); // Enough for encrypted phone number

                        // Note: We can't index encrypted data, so we remove the index on MobilePhone
                        // Ignore the computed MobilePhone property (not stored in DB)
                        entity.Ignore(e => e.MobilePhone);

                        entity.Property(e => e.Gender).IsRequired().HasMaxLength(20);
                        entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
                        entity.Property(e => e.Status).HasConversion<int>(); // Store enum as int
                        entity.Property(e => e.Notes).HasMaxLength(2000);

                        // Accident-related fields
                        entity.Property(e => e.Age);
                        entity.Property(e => e.Race).HasMaxLength(100);
                        entity.Property(e => e.AccidentAddress).HasColumnType("text");
                        entity.Property(e => e.AccidentDate);
                        entity.Property(e => e.VehicleDetails).HasColumnType("text");
                        entity.Property(e => e.DateReported);
                        entity.Property(e => e.PoliceCaseNumber).HasMaxLength(100);
                        entity.Property(e => e.AccidentDetails).HasColumnType("text");
                        entity.Property(e => e.RoadConditions).HasMaxLength(200);
                        entity.Property(e => e.DoctorsInformation).HasColumnType("text");
                        entity.Property(e => e.LawyersInformation).HasColumnType("text");
                        entity.Property(e => e.AdditionalNotes).HasColumnType("text");

                        // DateOfBirth is stored encrypted as string
                        entity.Property(e => e.DateOfBirthEncrypted)
                            .IsRequired()
                            .HasMaxLength(500); // Enough for encrypted DateTime

                        // Ignore the computed DateOfBirth property (not stored in DB)
                        entity.Ignore(e => e.DateOfBirth);

                        // Foreign key relationship to User (reviewer)
                        entity.HasOne(e => e.ReviewedByUser)
                        .WithMany()
                        .HasForeignKey(e => e.ReviewedByUserId)
                        .OnDelete(DeleteBehavior.SetNull);
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

                        // Performance indexes for common queries (critical for role-based filtering)
                        entity.HasIndex(e => e.AssignerId); // For finding patients assigned to a doctor/coordinator
                        entity.HasIndex(e => e.AssigneeId); // For finding doctors/coordinators assigned to a patient
                        entity.HasIndex(e => e.IsActive); // For filtering active assignments
                        entity.HasIndex(e => new { e.AssignerId, e.IsActive }); // Composite index for most common query pattern
                        entity.HasIndex(e => new { e.AssigneeId, e.IsActive }); // For reverse lookups
                  });

                  // Configure JournalEntry entity
                  modelBuilder.Entity<JournalEntry>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Text).IsRequired();
                        entity.Property(e => e.Mood).HasMaxLength(50);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);

                        // Foreign key relationship to patient (User)
                        entity.HasOne(e => e.User)
                        .WithMany(u => u.JournalEntries)
                        .HasForeignKey(e => e.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

                        // Foreign key relationship for who entered the entry (EnteredByUser)
                        entity.HasOne(e => e.EnteredByUser)
                        .WithMany()
                        .HasForeignKey(e => e.EnteredByUserId)
                        .OnDelete(DeleteBehavior.SetNull);

                        // Foreign key relationship for ignored by doctor
                        entity.HasOne(e => e.IgnoredByDoctor)
                        .WithMany()
                        .HasForeignKey(e => e.IgnoredByDoctorId)
                        .OnDelete(DeleteBehavior.SetNull);
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

                        entity.HasOne(e => e.IgnoredByDoctor)
                        .WithMany()
                        .HasForeignKey(e => e.IgnoredByDoctorId)
                        .OnDelete(DeleteBehavior.SetNull);
                  });

                  // Configure ChatMessage entity
                  modelBuilder.Entity<ChatMessage>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Content).IsRequired().HasColumnType("TEXT");
                        entity.Property(e => e.Metadata).HasMaxLength(1000);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
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
                        entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
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

                        entity.HasOne(e => e.ContentTypeModel)
                        .WithMany()
                        .HasForeignKey(e => e.ContentTypeModelId)
                        .OnDelete(DeleteBehavior.Restrict);

                        entity.HasOne(e => e.IgnoredByDoctor)
                        .WithMany()
                        .HasForeignKey(e => e.IgnoredByDoctorId)
                        .OnDelete(DeleteBehavior.SetNull);
                  });

                  // Configure ContentAnalysis entity
                  modelBuilder.Entity<ContentAnalysis>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.ContentTypeName).IsRequired().HasMaxLength(50);
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

                  // Configure ContentTypeModel entity
                  modelBuilder.Entity<ContentTypeModel>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                        entity.Property(e => e.Description).HasMaxLength(200);
                        entity.Property(e => e.Icon).HasMaxLength(20);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.SortOrder).HasDefaultValue(0);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Index for performance
                        entity.HasIndex(e => e.Name).IsUnique();
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => e.SortOrder);
                  });

                  // Configure ClinicalNote entity
                  modelBuilder.Entity<ClinicalNote>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                        entity.Property(e => e.Content).IsRequired().HasColumnType("TEXT");
                        entity.Property(e => e.NoteType).IsRequired().HasMaxLength(50);
                        entity.Property(e => e.Priority).IsRequired().HasMaxLength(20);
                        entity.Property(e => e.Tags).HasMaxLength(500);
                        entity.Property(e => e.IsConfidential).HasDefaultValue(false);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.IsIgnoredByDoctor).HasDefaultValue(false);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationships
                        entity.HasOne(e => e.Patient)
                        .WithMany()
                        .HasForeignKey(e => e.PatientId)
                        .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.Doctor)
                        .WithMany()
                        .HasForeignKey(e => e.DoctorId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Foreign key relationship for ignored by doctor
                        entity.HasOne(e => e.IgnoredByDoctor)
                        .WithMany()
                        .HasForeignKey(e => e.IgnoredByDoctorId)
                        .OnDelete(DeleteBehavior.SetNull);

                        // Indexes for performance
                        entity.HasIndex(e => e.PatientId);
                        entity.HasIndex(e => e.DoctorId);
                        entity.HasIndex(e => e.NoteType);
                        entity.HasIndex(e => e.Priority);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => e.IsIgnoredByDoctor);
                        entity.HasIndex(e => e.CreatedAt);
                  });

                  // Configure AIInstructionCategory entity
                  modelBuilder.Entity<AIInstructionCategory>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.Description).HasMaxLength(500);
                        entity.Property(e => e.Context).IsRequired().HasMaxLength(50).HasDefaultValue("HealthCheck");
                        entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Indexes for performance
                        entity.HasIndex(e => e.Context);
                        entity.HasIndex(e => e.DisplayOrder);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => new { e.Context, e.IsActive, e.DisplayOrder });
                  });

                  // Configure AIInstruction entity
                  modelBuilder.Entity<AIInstruction>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
                        entity.Property(e => e.Title).HasMaxLength(200);
                        entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationship
                        entity.HasOne(e => e.Category)
                        .WithMany(c => c.Instructions)
                        .HasForeignKey(e => e.CategoryId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Indexes for performance
                        entity.HasIndex(e => e.CategoryId);
                        entity.HasIndex(e => e.DisplayOrder);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => new { e.CategoryId, e.IsActive, e.DisplayOrder });
                  });

                  // Configure KnowledgeBaseCategory entity
                  modelBuilder.Entity<KnowledgeBaseCategory>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.Description).HasMaxLength(500);
                        entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Indexes for performance
                        entity.HasIndex(e => e.Name);
                        entity.HasIndex(e => e.DisplayOrder);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => new { e.IsActive, e.DisplayOrder });
                  });

                  // Configure KnowledgeBaseEntry entity
                  modelBuilder.Entity<KnowledgeBaseEntry>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                        entity.Property(e => e.Content).IsRequired().HasColumnType("TEXT");
                        entity.Property(e => e.Keywords).HasMaxLength(1000);
                        entity.Property(e => e.Priority).HasDefaultValue(0);
                        entity.Property(e => e.UseAsDirectResponse).HasDefaultValue(true);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationship
                        entity.HasOne(e => e.Category)
                        .WithMany(c => c.Entries)
                        .HasForeignKey(e => e.CategoryId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Foreign key relationships for created/updated by
                        entity.HasOne<User>()
                        .WithMany()
                        .HasForeignKey(e => e.CreatedByUserId)
                        .OnDelete(DeleteBehavior.SetNull);

                        entity.HasOne<User>()
                        .WithMany()
                        .HasForeignKey(e => e.UpdatedByUserId)
                        .OnDelete(DeleteBehavior.SetNull);

                        // Indexes for performance
                        entity.HasIndex(e => e.CategoryId);
                        entity.HasIndex(e => e.Priority);
                        entity.HasIndex(e => e.IsActive);
                        // Note: Index on Keywords removed - varchar(1000) exceeds MySQL key length limit
                        // Keywords are still searchable via application-level filtering
                        entity.HasIndex(e => new { e.CategoryId, e.IsActive, e.Priority });
                  });

                  // Configure AIResponseTemplate entity
                  modelBuilder.Entity<AIResponseTemplate>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.TemplateKey).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(200);
                        entity.Property(e => e.Content).IsRequired().HasColumnType("TEXT");
                        entity.Property(e => e.Description).HasMaxLength(500);
                        entity.Property(e => e.Priority).HasDefaultValue(0);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationships for created/updated by
                        entity.HasOne<User>()
                        .WithMany()
                        .HasForeignKey(e => e.CreatedByUserId)
                        .OnDelete(DeleteBehavior.SetNull);

                        entity.HasOne<User>()
                        .WithMany()
                        .HasForeignKey(e => e.UpdatedByUserId)
                        .OnDelete(DeleteBehavior.SetNull);

                        // Indexes for performance
                        entity.HasIndex(e => e.TemplateKey).IsUnique();
                        entity.HasIndex(e => e.Priority);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => new { e.IsActive, e.Priority });
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

                  // Configure SmsMessage entity
                  modelBuilder.Entity<SmsMessage>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
                        entity.Property(e => e.SentAt).IsRequired();
                        entity.Property(e => e.IsRead).HasDefaultValue(false);

                        // Foreign key relationships
                        entity.HasOne(e => e.Sender)
                        .WithMany()
                        .HasForeignKey(e => e.SenderId)
                        .OnDelete(DeleteBehavior.Restrict);

                        entity.HasOne(e => e.Receiver)
                        .WithMany()
                        .HasForeignKey(e => e.ReceiverId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Indexes for performance
                        entity.HasIndex(e => new { e.SenderId, e.ReceiverId, e.SentAt });
                        entity.HasIndex(e => new { e.ReceiverId, e.IsRead });
                  });

                  // Configure Appointment entity
                  modelBuilder.Entity<Appointment>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.AppointmentDateTime).IsRequired();
                        entity.Property(e => e.Duration).IsRequired();
                        entity.Property(e => e.AppointmentType).HasConversion<int>();
                        entity.Property(e => e.Status).HasConversion<int>();
                        entity.Property(e => e.Reason).HasMaxLength(500);
                        entity.Property(e => e.Notes).HasMaxLength(2000);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.DayBeforeReminderSent).HasDefaultValue(false);
                        entity.Property(e => e.DayOfReminderSent).HasDefaultValue(false);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationships
                        entity.HasOne(e => e.Doctor)
                        .WithMany()
                        .HasForeignKey(e => e.DoctorId)
                        .OnDelete(DeleteBehavior.Restrict);

                        entity.HasOne(e => e.Patient)
                        .WithMany()
                        .HasForeignKey(e => e.PatientId)
                        .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.CreatedByUser)
                        .WithMany()
                        .HasForeignKey(e => e.CreatedByUserId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Indexes for performance
                        entity.HasIndex(e => e.DoctorId);
                        entity.HasIndex(e => e.PatientId);
                        entity.HasIndex(e => e.AppointmentDateTime);
                        entity.HasIndex(e => new { e.DoctorId, e.AppointmentDateTime });
                        entity.HasIndex(e => e.Status);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => e.AppointmentType);
                  });

                  // Configure DoctorAvailability entity
                  modelBuilder.Entity<DoctorAvailability>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Date).IsRequired();
                        entity.Property(e => e.IsOutOfOffice).HasDefaultValue(false);
                        entity.Property(e => e.Reason).HasMaxLength(500);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationship
                        entity.HasOne(e => e.Doctor)
                        .WithMany()
                        .HasForeignKey(e => e.DoctorId)
                        .OnDelete(DeleteBehavior.Cascade);

                        // Unique constraint: one availability record per doctor per day
                        entity.HasIndex(e => new { e.DoctorId, e.Date }).IsUnique();

                        // Indexes for performance
                        entity.HasIndex(e => e.DoctorId);
                        entity.HasIndex(e => e.Date);
                        entity.HasIndex(e => e.IsOutOfOffice);
                  });

                  // Configure CriticalValueCategory entity
                  modelBuilder.Entity<CriticalValueCategory>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                        entity.Property(e => e.Description).HasMaxLength(500);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Indexes for performance
                        entity.HasIndex(e => e.Name);
                        entity.HasIndex(e => e.IsActive);
                  });

                  // Configure CriticalValuePattern entity
                  modelBuilder.Entity<CriticalValuePattern>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Pattern).IsRequired().HasMaxLength(500);
                        entity.Property(e => e.Description).HasMaxLength(500);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationship
                        entity.HasOne(e => e.Category)
                        .WithMany(c => c.Patterns)
                        .HasForeignKey(e => e.CategoryId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Indexes for performance
                        entity.HasIndex(e => e.CategoryId);
                        entity.HasIndex(e => e.IsActive);
                  });

                  // Configure CriticalValueKeyword entity
                  modelBuilder.Entity<CriticalValueKeyword>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Keyword).IsRequired().HasMaxLength(500);
                        entity.Property(e => e.Description).HasMaxLength(500);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationship
                        entity.HasOne(e => e.Category)
                        .WithMany()
                        .HasForeignKey(e => e.CategoryId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Indexes for performance
                        entity.HasIndex(e => e.CategoryId);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => e.Keyword);
                  });

                  // Configure GenericQuestionPattern entity
                  modelBuilder.Entity<GenericQuestionPattern>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Pattern).IsRequired().HasMaxLength(500);
                        entity.Property(e => e.Description).HasMaxLength(500);
                        entity.Property(e => e.Priority).HasDefaultValue(0);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Indexes for performance
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => e.Priority);
                        entity.HasIndex(e => new { e.IsActive, e.Priority });
                  });

                  // Configure MedicalThreshold entity
                  modelBuilder.Entity<MedicalThreshold>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.ParameterName).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.Unit).HasMaxLength(50);
                        entity.Property(e => e.SeverityLevel).HasMaxLength(50);
                        entity.Property(e => e.ComparisonOperator).HasMaxLength(20);
                        entity.Property(e => e.SecondaryParameterName).HasMaxLength(100);
                        entity.Property(e => e.SecondaryComparisonOperator).HasMaxLength(20);
                        entity.Property(e => e.Description).HasMaxLength(500);
                        entity.Property(e => e.Priority).HasDefaultValue(0);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Indexes for performance
                        entity.HasIndex(e => e.ParameterName);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => e.Priority);
                        entity.HasIndex(e => e.SeverityLevel);
                        entity.HasIndex(e => new { e.ParameterName, e.IsActive });
                        entity.HasIndex(e => new { e.IsActive, e.Priority });
                  });

                  // Configure SectionMarker entity
                  modelBuilder.Entity<SectionMarker>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Marker).IsRequired().HasMaxLength(500);
                        entity.Property(e => e.Description).HasMaxLength(500);
                        entity.Property(e => e.Category).HasMaxLength(100);
                        entity.Property(e => e.Priority).HasDefaultValue(0);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Indexes for performance
                        entity.HasIndex(e => e.Marker).IsUnique();
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => e.Category);
                        entity.HasIndex(e => e.Priority);
                        entity.HasIndex(e => new { e.IsActive, e.Priority });
                  });

                  // Configure AIModelConfig entity
                  modelBuilder.Entity<AIModelConfig>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.ModelName).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.ModelType).IsRequired().HasMaxLength(50);
                        entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
                        entity.Property(e => e.ApiEndpoint).IsRequired().HasMaxLength(500);
                        entity.Property(e => e.ApiKeyConfigKey).HasMaxLength(100);
                        entity.Property(e => e.Context).IsRequired().HasMaxLength(50).HasDefaultValue("ClinicalNote");
                        entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Unique constraint on ModelName + Context
                        entity.HasIndex(e => new { e.ModelName, e.Context }).IsUnique();
                        entity.HasIndex(e => e.ModelType);
                        entity.HasIndex(e => e.Context);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => new { e.Context, e.IsActive, e.DisplayOrder });
                  });

                  // Configure AIModelChain entity
                  modelBuilder.Entity<AIModelChain>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.ChainName).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.Context).IsRequired().HasMaxLength(50).HasDefaultValue("ClinicalNote");
                        entity.Property(e => e.Description).HasMaxLength(500);
                        entity.Property(e => e.ChainOrder).HasDefaultValue(1);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationships
                        entity.HasOne(e => e.PrimaryModel)
                              .WithMany()
                              .HasForeignKey(e => e.PrimaryModelId)
                              .OnDelete(DeleteBehavior.Restrict);

                        entity.HasOne(e => e.SecondaryModel)
                              .WithMany()
                              .HasForeignKey(e => e.SecondaryModelId)
                              .OnDelete(DeleteBehavior.Restrict);

                        // Unique constraint on ChainName + Context
                        entity.HasIndex(e => new { e.ChainName, e.Context }).IsUnique();
                        entity.HasIndex(e => e.Context);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => e.PrimaryModelId);
                        entity.HasIndex(e => e.SecondaryModelId);
                  });

                  // Configure ServiceRequest entity
                  modelBuilder.Entity<ServiceRequest>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                        entity.Property(e => e.Type).HasMaxLength(100);
                        entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Active");
                        entity.Property(e => e.Description).HasMaxLength(1000);
                        entity.Property(e => e.ServiceZipCode).HasMaxLength(10);
                        entity.Property(e => e.MaxDistanceMiles).HasDefaultValue(50);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationship to PrimaryExpertise
                        entity.HasOne(e => e.PrimaryExpertise)
                              .WithMany()
                              .HasForeignKey(e => e.PrimaryExpertiseId)
                              .OnDelete(DeleteBehavior.SetNull);

                        // Foreign key relationships
                        entity.HasOne(e => e.Client)
                        .WithMany()
                        .HasForeignKey(e => e.ClientId)
                        .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.CreatedByUser)
                        .WithMany()
                        .HasForeignKey(e => e.CreatedByUserId)
                        .OnDelete(DeleteBehavior.SetNull);

                        // Indexes for performance
                        entity.HasIndex(e => e.ClientId);
                        entity.HasIndex(e => e.Status);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => e.CreatedAt);
                        entity.HasIndex(e => new { e.ClientId, e.IsActive });
                  });

                  // Configure ServiceRequestAssignment entity
                  modelBuilder.Entity<ServiceRequestAssignment>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.AssignedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationships
                        entity.HasOne(e => e.ServiceRequest)
                        .WithMany(sr => sr.Assignments)
                        .HasForeignKey(e => e.ServiceRequestId)
                        .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.SmeUser)
                        .WithMany()
                        .HasForeignKey(e => e.SmeUserId)
                        .OnDelete(DeleteBehavior.Restrict);

                        entity.HasOne(e => e.AssignedByUser)
                        .WithMany()
                        .HasForeignKey(e => e.AssignedByUserId)
                        .OnDelete(DeleteBehavior.SetNull);

                        // Indexes for performance
                        entity.HasIndex(e => e.ServiceRequestId);
                        entity.HasIndex(e => e.SmeUserId);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => new { e.ServiceRequestId, e.IsActive });
                        entity.HasIndex(e => new { e.SmeUserId, e.IsActive });
                  });

                  // Add ServiceRequestId foreign keys to content entities
                  // ClinicalNote
                  modelBuilder.Entity<ClinicalNote>(entity =>
                  {
                        entity.HasOne(e => e.ServiceRequest)
                        .WithMany()
                        .HasForeignKey(e => e.ServiceRequestId)
                        .OnDelete(DeleteBehavior.SetNull);

                        entity.HasIndex(e => e.ServiceRequestId);
                  });

                  // ContentItem
                  modelBuilder.Entity<ContentItem>(entity =>
                  {
                        entity.HasOne(e => e.ServiceRequest)
                        .WithMany()
                        .HasForeignKey(e => e.ServiceRequestId)
                        .OnDelete(DeleteBehavior.SetNull);

                        entity.HasIndex(e => e.ServiceRequestId);
                  });

                  // JournalEntry
                  modelBuilder.Entity<JournalEntry>(entity =>
                  {
                        entity.HasOne(e => e.ServiceRequest)
                        .WithMany()
                        .HasForeignKey(e => e.ServiceRequestId)
                        .OnDelete(DeleteBehavior.SetNull);

                        entity.HasIndex(e => e.ServiceRequestId);
                  });

                  // ChatSession
                  modelBuilder.Entity<ChatSession>(entity =>
                  {
                        entity.HasOne(e => e.ServiceRequest)
                        .WithMany()
                        .HasForeignKey(e => e.ServiceRequestId)
                        .OnDelete(DeleteBehavior.SetNull);

                        entity.HasIndex(e => e.ServiceRequestId);
                  });

                  // Appointment
                  modelBuilder.Entity<Appointment>(entity =>
                  {
                        entity.HasOne(e => e.ServiceRequest)
                        .WithMany()
                        .HasForeignKey(e => e.ServiceRequestId)
                        .OnDelete(DeleteBehavior.SetNull);

                        entity.HasIndex(e => e.ServiceRequestId);
                  });

                  // ContentAlert
                  modelBuilder.Entity<ContentAlert>(entity =>
                  {
                        entity.HasOne(e => e.ServiceRequest)
                        .WithMany()
                        .HasForeignKey(e => e.ServiceRequestId)
                        .OnDelete(DeleteBehavior.SetNull);

                        entity.HasIndex(e => e.ServiceRequestId);
                  });

                  // Configure Expertise entity
                  modelBuilder.Entity<Expertise>(entity =>
                  {
                        entity.ToTable("Expertise"); // Explicit table name (singular, matches SQL script)
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.Description).HasMaxLength(500);
                        entity.HasIndex(e => e.Name);
                        entity.HasIndex(e => e.IsActive);
                  });

                  // Configure SmeExpertise entity
                  modelBuilder.Entity<SmeExpertise>(entity =>
                  {
                        entity.ToTable("SmeExpertise"); // Explicit table name (singular, matches SQL script)
                        entity.HasKey(e => e.Id);
                        entity.HasIndex(e => e.SmeUserId);
                        entity.HasIndex(e => e.ExpertiseId);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => new { e.SmeUserId, e.ExpertiseId }).IsUnique();

                        entity.HasOne(e => e.SmeUser)
                              .WithMany(u => u.SmeExpertises)
                              .HasForeignKey(e => e.SmeUserId)
                              .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.Expertise)
                              .WithMany(ex => ex.SmeExpertises)
                              .HasForeignKey(e => e.ExpertiseId)
                              .OnDelete(DeleteBehavior.Cascade);
                  });

                  // Configure ServiceRequestExpertise entity
                  modelBuilder.Entity<ServiceRequestExpertise>(entity =>
                  {
                        entity.ToTable("ServiceRequestExpertise"); // Explicit table name (singular, matches SQL script)
                        entity.HasKey(e => e.Id);
                        entity.HasIndex(e => e.ServiceRequestId);
                        entity.HasIndex(e => e.ExpertiseId);
                        entity.HasIndex(e => new { e.ServiceRequestId, e.ExpertiseId }).IsUnique();

                        entity.HasOne(e => e.ServiceRequest)
                              .WithMany(sr => sr.Expertises)
                              .HasForeignKey(e => e.ServiceRequestId)
                              .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.Expertise)
                              .WithMany(ex => ex.ServiceRequestExpertises)
                              .HasForeignKey(e => e.ExpertiseId)
                              .OnDelete(DeleteBehavior.Cascade);
                  });

                  // Configure ZipCodeLookup entity
                  modelBuilder.Entity<ZipCodeLookup>(entity =>
                  {
                        entity.ToTable("ZipCodeLookup"); // Explicit table name (matches SQL script)
                        entity.HasKey(e => e.ZipCode);
                        entity.Property(e => e.ZipCode).IsRequired().HasMaxLength(10);
                        entity.Property(e => e.Latitude).IsRequired().HasColumnType("DECIMAL(10, 8)");
                        entity.Property(e => e.Longitude).IsRequired().HasColumnType("DECIMAL(11, 8)");
                        entity.Property(e => e.City).HasMaxLength(100);
                        entity.Property(e => e.State).HasMaxLength(2);
                        entity.HasIndex(e => e.State);
                  });

                  // Configure BillingAccount entity
                  modelBuilder.Entity<BillingAccount>(entity =>
                  {
                        entity.ToTable("BillingAccounts");
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
                        entity.Property(e => e.Name).HasMaxLength(255);
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationships
                        entity.HasOne(e => e.Company)
                              .WithMany()
                              .HasForeignKey(e => e.CompanyId)
                              .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.User)
                              .WithMany()
                              .HasForeignKey(e => e.UserId)
                              .OnDelete(DeleteBehavior.Cascade);

                        // Indexes
                        entity.HasIndex(e => e.Type);
                        entity.HasIndex(e => e.IsActive);
                        entity.HasIndex(e => e.CompanyId).IsUnique();
                        entity.HasIndex(e => e.UserId).IsUnique();
                  });

                  // Configure BillingRate entity
                  modelBuilder.Entity<BillingRate>(entity =>
                  {
                        entity.ToTable("BillingRates");
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Amount).IsRequired().HasColumnType("DECIMAL(10,2)");
                        entity.Property(e => e.IsActive).HasDefaultValue(true);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        // Foreign key relationships
                        entity.HasOne(e => e.BillingAccount)
                              .WithMany(ba => ba.BillingRates)
                              .HasForeignKey(e => e.BillingAccountId)
                              .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.Expertise)
                              .WithMany()
                              .HasForeignKey(e => e.ExpertiseId)
                              .OnDelete(DeleteBehavior.Cascade);

                        // Unique constraint: one rate per (BillingAccount, Expertise)
                        entity.HasIndex(e => new { e.BillingAccountId, e.ExpertiseId }).IsUnique();
                        entity.HasIndex(e => e.ExpertiseId);
                        entity.HasIndex(e => e.IsActive);
                  });

                  // Configure ServiceRequestCharge entity (update for new fields)
                  modelBuilder.Entity<ServiceRequestCharge>(entity =>
                  {
                        entity.ToTable("ServiceRequestCharges");
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.RateSource).IsRequired().HasMaxLength(50).HasDefaultValue("Default");
                        entity.Property(e => e.Amount).IsRequired().HasColumnType("DECIMAL(18,2)");
                        entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Ready");

                        // Foreign key relationships
                        entity.HasOne(e => e.ServiceRequest)
                              .WithMany()
                              .HasForeignKey(e => e.ServiceRequestId)
                              .OnDelete(DeleteBehavior.Restrict);

                        entity.HasOne(e => e.Expertise)
                              .WithMany()
                              .HasForeignKey(e => e.ExpertiseId)
                              .OnDelete(DeleteBehavior.SetNull);

                        // Unique constraint: one charge per (ServiceRequest, BillingAccount)
                        entity.HasIndex(e => new { e.ServiceRequestId, e.BillingAccountId }).IsUnique();
                        entity.HasIndex(e => e.BillingAccountId);
                        entity.HasIndex(e => e.ExpertiseId);
                        entity.HasIndex(e => e.Status);
                  });

                  // Configure ClientProfile entity
                  modelBuilder.Entity<ClientProfile>(entity =>
                  {
                        entity.ToTable("ClientProfiles");
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.CommunicationStyle).HasMaxLength(50).HasDefaultValue("Balanced");
                        entity.Property(e => e.InformationTolerance).IsRequired().HasColumnType("DECIMAL(3,2)").HasDefaultValue(0.5m);
                        entity.Property(e => e.EmotionalSensitivity).IsRequired().HasColumnType("DECIMAL(3,2)").HasDefaultValue(0.5m);
                        entity.Property(e => e.PreferredTone).HasMaxLength(50).HasDefaultValue("Supportive");
                        entity.Property(e => e.TotalInteractions).HasDefaultValue(0);
                        entity.Property(e => e.SuccessfulResolutions).HasDefaultValue(0);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        entity.HasOne(e => e.Client)
                              .WithMany()
                              .HasForeignKey(e => e.ClientId)
                              .OnDelete(DeleteBehavior.Cascade);

                        entity.HasIndex(e => e.ClientId).IsUnique();
                        entity.HasIndex(e => e.LastUpdated);
                  });

                  // Configure ClientInteractionPattern entity
                  modelBuilder.Entity<ClientInteractionPattern>(entity =>
                  {
                        entity.ToTable("ClientInteractionPatterns");
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.PatternType).IsRequired().HasMaxLength(50);
                        entity.Property(e => e.PatternData).HasColumnType("JSON");
                        entity.Property(e => e.Confidence).IsRequired().HasColumnType("DECIMAL(3,2)").HasDefaultValue(0.5m);
                        entity.Property(e => e.OccurrenceCount).HasDefaultValue(1);
                        entity.Property(e => e.LastObserved).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        entity.HasOne(e => e.ClientProfile)
                              .WithMany(p => p.InteractionPatterns)
                              .HasForeignKey(e => e.ClientId)
                              .OnDelete(DeleteBehavior.Cascade);

                        entity.HasIndex(e => new { e.ClientId, e.PatternType });
                        entity.HasIndex(e => e.LastObserved);
                  });

                  // Configure ClientKeywordReaction entity
                  modelBuilder.Entity<ClientKeywordReaction>(entity =>
                  {
                        entity.ToTable("ClientKeywordReactions");
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Keyword).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.ReactionScore).HasDefaultValue(0);
                        entity.Property(e => e.OccurrenceCount).HasDefaultValue(1);
                        entity.Property(e => e.LastSeen).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        entity.HasOne(e => e.ClientProfile)
                              .WithMany(p => p.KeywordReactions)
                              .HasForeignKey(e => e.ClientId)
                              .OnDelete(DeleteBehavior.Cascade);

                        entity.HasIndex(e => new { e.ClientId, e.Keyword }).IsUnique();
                        entity.HasIndex(e => e.ReactionScore);
                  });

                  // Configure ClientServicePreference entity
                  modelBuilder.Entity<ClientServicePreference>(entity =>
                  {
                        entity.ToTable("ClientServicePreferences");
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.ServiceType).IsRequired().HasMaxLength(100);
                        entity.Property(e => e.PreferenceScore).IsRequired().HasColumnType("DECIMAL(3,2)").HasDefaultValue(0.5m);
                        entity.Property(e => e.RequestCount).HasDefaultValue(0);
                        entity.Property(e => e.SuccessRate).HasColumnType("DECIMAL(3,2)");
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                        entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAddOrUpdate();

                        entity.HasOne(e => e.ClientProfile)
                              .WithMany(p => p.ServicePreferences)
                              .HasForeignKey(e => e.ClientId)
                              .OnDelete(DeleteBehavior.Cascade);

                        entity.HasIndex(e => new { e.ClientId, e.ServiceType }).IsUnique();
                        entity.HasIndex(e => e.PreferenceScore);
                  });

                  // Configure ClientInteractionHistory entity
                  modelBuilder.Entity<ClientInteractionHistory>(entity =>
                  {
                        entity.ToTable("ClientInteractionHistory");
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.InteractionType).IsRequired().HasMaxLength(50);
                        entity.Property(e => e.ClientMessage).HasColumnType("TEXT");
                        entity.Property(e => e.AgentResponse).HasColumnType("TEXT");
                        entity.Property(e => e.Sentiment).HasMaxLength(50);
                        entity.Property(e => e.Urgency).HasMaxLength(50);
                        entity.Property(e => e.InformationLevel).HasMaxLength(50);
                        entity.Property(e => e.ClientReaction).HasMaxLength(50);
                        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                        entity.HasOne(e => e.ClientProfile)
                              .WithMany()
                              .HasForeignKey(e => e.ClientId)
                              .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.ServiceRequest)
                              .WithMany()
                              .HasForeignKey(e => e.ServiceRequestId)
                              .OnDelete(DeleteBehavior.SetNull);

                        entity.HasIndex(e => new { e.ClientId, e.CreatedAt });
                        entity.HasIndex(e => e.ServiceRequestId);
                  });
            }
      }
}
