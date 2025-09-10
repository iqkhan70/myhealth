namespace SM_MentalHealthApp.Shared
{
    public class Patient
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public List<JournalEntry> JournalEntries { get; set; } = new();
        public List<ChatSession> ChatSessions { get; set; } = new();
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
}
