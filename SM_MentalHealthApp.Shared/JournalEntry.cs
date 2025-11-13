using System.Text.Json.Serialization;

namespace SM_MentalHealthApp.Shared
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public List<User> Users { get; set; } = new();
    }

    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? MobilePhone { get; set; } // Mobile phone for SMS alerts
        public int RoleId { get; set; } = 1; // Default to Patient role
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFirstLogin { get; set; } = true;
        public bool MustChangePassword { get; set; } = false;

        // Doctor-specific fields (nullable for non-doctors)
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }

        // Navigation properties
        [JsonIgnore]
        public Role Role { get; set; } = null!;
        [JsonIgnore]
        public List<JournalEntry> JournalEntries { get; set; } = new();
        [JsonIgnore]
        public List<ChatSession> ChatSessions { get; set; } = new();
        [JsonIgnore]
        public List<UserAssignment> Assignments { get; set; } = new(); // As assigner
        [JsonIgnore]
        public List<UserAssignment> AssignedTo { get; set; } = new(); // As assignee

        public string FullName => $"{FirstName} {LastName} ({Email})";
        public string StatusText => IsActive ? "Active" : "Inactive";

    }

    public class UserAssignment
    {
        public int AssignerId { get; set; }
        public int AssigneeId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public User Assigner { get; set; } = null!;
        public User Assignee { get; set; } = null!;
    }

    public class JournalEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; } // The patient whose journal this entry belongs to
        public int? EnteredByUserId { get; set; } // Who actually entered this (null if patient entered for themselves)
        public string Text { get; set; } = string.Empty;
        public string? AIResponse { get; set; }
        public string? Mood { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true; // Soft delete flag

        // Doctor ignore functionality (for AI analysis exclusion)
        public bool IsIgnoredByDoctor { get; set; } = false;
        public int? IgnoredByDoctorId { get; set; }
        public DateTime? IgnoredAt { get; set; }

        // Navigation properties
        public User? User { get; set; } // The patient
        public User? EnteredByUser { get; set; } // Who entered it (doctor or patient)
        public User? IgnoredByDoctor { get; set; } // Doctor who ignored this entry
    }


    public class ChatResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }

    public enum AiProvider
    {
        OpenAI,
        Ollama,
        CustomKnowledge,
        HuggingFace
    }

    public class UserStats
    {
        public int UserId { get; set; }
        public int TotalJournalEntries { get; set; }
        public int EntriesLast30Days { get; set; }
        public string AverageMood { get; set; } = string.Empty;
        public Dictionary<string, int> MoodDistribution { get; set; } = new();
        public DateTime? LastEntryDate { get; set; }
        public string MostCommonMood { get; set; } = string.Empty;
        public int TotalChatSessions { get; set; }
    }

    // Authentication Models
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }
    }

    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? MobilePhone { get; set; } // Mobile phone for SMS alerts
        public int RoleId { get; set; } = 1; // Default to Patient role

        // Doctor-specific fields (optional)
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
    }

    public class AuthUser
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public int RoleId { get; set; } = 1; // Default to Patient role
        public string RoleName { get; set; } = "Patient";
        public bool IsFirstLogin { get; set; }
        public bool MustChangePassword { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ContentItem
    {
        public int Id { get; set; }
        public Guid ContentGuid { get; set; } = Guid.NewGuid();
        public int PatientId { get; set; }
        public int? AddedByUserId { get; set; } // Who added this content (null if patient added for themselves)
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty; // MIME type
        public long FileSizeBytes { get; set; }
        public string S3Bucket { get; set; } = string.Empty;
        public string S3Key { get; set; } = string.Empty; // S3 object key
        // public string S3Url { get; set; } = string.Empty; // Removed - URLs generated on-demand for security
        public int ContentTypeModelId { get; set; } // Foreign key to ContentTypeModel
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastAccessedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Doctor ignore functionality - allows doctors to mark historical data as ignored for AI analysis
        public bool IsIgnoredByDoctor { get; set; } = false;
        public int? IgnoredByDoctorId { get; set; } // Which doctor marked this as ignored
        public DateTime? IgnoredAt { get; set; } // When it was ignored

        // Navigation properties
        public User Patient { get; set; } = null!;
        public User? AddedByUser { get; set; }
        public User? IgnoredByDoctor { get; set; } // Navigation to the doctor who ignored it
        public ContentTypeModel ContentTypeModel { get; set; } = null!;
    }

    public enum ContentTypeEnum
    {
        Document = 1,
        Image = 2,
        Video = 3,
        Audio = 4,
        Other = 5
    }

    // Doctor Assignment Request Models
    public class DoctorAssignPatientRequest
    {
        public int PatientId { get; set; }
        public int ToDoctorId { get; set; }
    }

    public class DoctorUnassignPatientRequest
    {
        public int PatientId { get; set; }
    }

    // Patient Request Models
    public class CreatePatientRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? MobilePhone { get; set; }
    }

    public class UpdatePatientRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? MobilePhone { get; set; }
        public string? Password { get; set; }
    }

    // AI Health Check Result
    public class AiHealthCheckResult
    {
        public bool Success { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string AiResponse { get; set; } = string.Empty;
        public int AlertsSent { get; set; }
        public int DoctorsNotified { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SeveritySource> SeveritySources { get; set; } = new();
    }

    public class SeveritySource
    {
        public string SourceType { get; set; } = string.Empty; // "JournalEntry", "ClinicalNote", "Content", "Emergency"
        public int SourceId { get; set; }
        public string SourceTitle { get; set; } = string.Empty;
        public string SourcePreview { get; set; } = string.Empty;
        public DateTime SourceDate { get; set; }
        public string ContributionReason { get; set; } = string.Empty; // Why this source contributed to severity
        public string NavigationRoute { get; set; } = string.Empty; // Route to navigate to this source
    }

    // Content Analysis Models
    public class ContentAnalysis
    {
        public int Id { get; set; }
        public int ContentId { get; set; }
        public string ContentTypeName { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public Dictionary<string, object> AnalysisResults { get; set; } = new();
        public List<string> Alerts { get; set; } = new();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string ProcessingStatus { get; set; } = "Pending";
        public string? ErrorMessage { get; set; }

        // Navigation properties
        public ContentItem Content { get; set; } = null!;
    }

    public class ContentAlert
    {
        public int Id { get; set; }
        public int ContentId { get; set; }
        public int PatientId { get; set; }
        public string AlertType { get; set; } = string.Empty; // "Critical", "Warning", "Info"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // "High", "Medium", "Low"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public bool IsResolved { get; set; } = false;

        // Navigation properties
        public ContentItem Content { get; set; } = null!;
        public User Patient { get; set; } = null!;
    }

    // Medical Document Analysis Models
    public class MedicalDocumentAnalysis
    {
        public string DocumentType { get; set; } = string.Empty; // "Lab Report", "X-Ray", "Prescription", etc.
        public List<string> Medications { get; set; } = new();
        public List<string> Symptoms { get; set; } = new();
        public List<string> Diagnoses { get; set; } = new();
        public List<string> VitalSigns { get; set; } = new();
        public List<string> TestResults { get; set; } = new();
        public Dictionary<string, string> KeyValues { get; set; } = new(); // Key-value pairs from documents
        public string Summary { get; set; } = string.Empty;
        public List<string> Concerns { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    // Video Analysis Models
    public class VideoAnalysis
    {
        public List<string> ExtractedText { get; set; } = new(); // Text from video frames
        public List<string> DetectedObjects { get; set; } = new(); // Objects detected in video
        public List<string> DetectedActivities { get; set; } = new(); // Activities detected
        public List<string> AudioTranscription { get; set; } = new(); // Speech-to-text results
        public string Summary { get; set; } = string.Empty;
        public List<string> KeyMoments { get; set; } = new(); // Important moments in video
    }

    // Audio Analysis Models
    public class AudioAnalysis
    {
        public string Transcription { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public string Sentiment { get; set; } = string.Empty;
        public List<string> Emotions { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public List<string> Concerns { get; set; } = new();
    }
}
