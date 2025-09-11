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
        public List<Patient> Patients { get; set; } = new();
        public List<Doctor> Doctors { get; set; } = new();
    }

    public class Patient
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public int RoleId { get; set; } = 1; // Default to Patient role
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFirstLogin { get; set; } = true;
        public bool MustChangePassword { get; set; } = false;
        
        // Navigation properties
        public Role Role { get; set; } = null!;
        public List<JournalEntry> JournalEntries { get; set; } = new();
        public List<ChatSession> ChatSessions { get; set; } = new();
        public List<DoctorPatient> DoctorPatients { get; set; } = new();
    }

    public class Doctor
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public int RoleId { get; set; } = 2; // Default to Doctor role
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFirstLogin { get; set; } = true;
        public bool MustChangePassword { get; set; } = false;
        
        // Navigation properties
        public Role Role { get; set; } = null!;
        public List<DoctorPatient> DoctorPatients { get; set; } = new();
    }

    public class DoctorPatient
    {
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public Doctor Doctor { get; set; } = null!;
        public Patient Patient { get; set; } = null!;
    }

    public class JournalEntry
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? AIResponse { get; set; }
        public string? Mood { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public Patient? Patient { get; set; }
    }

    public class ChatSession
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActivityAt { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Navigation property
        public Patient? Patient { get; set; }
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

    public class PatientStats
    {
        public int PatientId { get; set; }
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
        public Patient? Patient { get; set; }
    }

    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
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
}
