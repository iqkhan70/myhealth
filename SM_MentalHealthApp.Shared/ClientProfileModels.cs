using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SM_MentalHealthApp.Shared
{
    // Enums for client profile system
    public enum CommunicationStyle
    {
        Detailed,      // Prefers comprehensive information
        Brief,          // Prefers concise responses
        Technical,      // Prefers technical details
        Simple,         // Prefers simple explanations
        Balanced        // Default - adapts based on context
    }

    public enum PreferredTone
    {
        Supportive,    // Warm, empathetic
        Professional,  // Formal, business-like
        Casual,        // Friendly, informal
        Technical      // Factual, precise
    }

    public enum InformationLevel
    {
        Minimal,       // Very brief, essential only
        Moderate,      // Balanced information
        Detailed,      // Comprehensive information
        TooLittle,     // Client needs more information
        TooMuch        // Client is overwhelmed
    }

    public enum Urgency
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum Sentiment
    {
        Positive,
        Neutral,
        Negative,
        Frustrated,
        Panic
    }

    public enum ClientReaction
    {
        Satisfied,
        Confused,
        Frustrated,
        Overwhelmed,
        Relieved,
        Neutral
    }

    // Main Client Profile entity
    public class ClientProfile
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        
        [MaxLength(50)]
        public string CommunicationStyle { get; set; } = "Balanced";
        
        [Range(0, 1)]
        public decimal InformationTolerance { get; set; } = 0.5m; // 0-1 scale
        
        [Range(0, 1)]
        public decimal EmotionalSensitivity { get; set; } = 0.5m; // 0-1 scale
        
        [MaxLength(50)]
        public string? PreferredTone { get; set; } = "Supportive";
        
        public int TotalInteractions { get; set; } = 0;
        public int SuccessfulResolutions { get; set; } = 0;
        public int? AverageResponseTime { get; set; } // in seconds
        
        public DateTime? LastUpdated { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [JsonIgnore]
        public User? Client { get; set; }
        
        [JsonIgnore]
        public List<ClientInteractionPattern> InteractionPatterns { get; set; } = new();
        
        [JsonIgnore]
        public List<ClientKeywordReaction> KeywordReactions { get; set; } = new();
        
        [JsonIgnore]
        public List<ClientServicePreference> ServicePreferences { get; set; } = new();
    }

    // Interaction patterns learned from client behavior
    public class ClientInteractionPattern
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        
        [MaxLength(50)]
        public string PatternType { get; set; } = string.Empty; // UrgencyResponse, InfoPreference, etc.
        
        public string? PatternData { get; set; } // JSON data
        
        [Range(0, 1)]
        public decimal Confidence { get; set; } = 0.5m;
        
        public int OccurrenceCount { get; set; } = 1;
        public DateTime LastObserved { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [JsonIgnore]
        public ClientProfile? ClientProfile { get; set; }
    }

    // Keyword reactions - tracks how clients react to specific words/phrases
    public class ClientKeywordReaction
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        
        [MaxLength(100)]
        public string Keyword { get; set; } = string.Empty;
        
        public int ReactionScore { get; set; } = 0; // Positive increases, negative decreases
        public int OccurrenceCount { get; set; } = 1;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [JsonIgnore]
        public ClientProfile? ClientProfile { get; set; }
    }

    // Service preferences - tracks which service types client prefers
    public class ClientServicePreference
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        
        [MaxLength(100)]
        public string ServiceType { get; set; } = string.Empty; // Plumbing, Car Repair, etc.
        
        [Range(0, 1)]
        public decimal PreferenceScore { get; set; } = 0.5m;
        
        public int RequestCount { get; set; } = 0;
        
        [Range(0, 1)]
        public decimal? SuccessRate { get; set; }
        
        public DateTime? LastRequestDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [JsonIgnore]
        public ClientProfile? ClientProfile { get; set; }
    }

    // Interaction history - detailed log of all interactions for learning
    public class ClientInteractionHistory
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int? ServiceRequestId { get; set; }
        
        [MaxLength(50)]
        public string InteractionType { get; set; } = string.Empty; // Message, Response, Action
        
        public string? ClientMessage { get; set; }
        public string? AgentResponse { get; set; }
        
        [MaxLength(50)]
        public string? Sentiment { get; set; }
        
        [MaxLength(50)]
        public string? Urgency { get; set; }
        
        [MaxLength(50)]
        public string? InformationLevel { get; set; }
        
        [MaxLength(50)]
        public string? ClientReaction { get; set; }
        
        public int? ResponseTime { get; set; } // in seconds
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [JsonIgnore]
        public ClientProfile? ClientProfile { get; set; }
        
        [JsonIgnore]
        public ServiceRequest? ServiceRequest { get; set; }
    }

    // DTOs for API responses
    public class ClientProfileDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string CommunicationStyle { get; set; } = string.Empty;
        public decimal InformationTolerance { get; set; }
        public decimal EmotionalSensitivity { get; set; }
        public string? PreferredTone { get; set; }
        public int TotalInteractions { get; set; }
        public int SuccessfulResolutions { get; set; }
        public int? AverageResponseTime { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Analysis results from message processing
    public class MessageAnalysis
    {
        public Sentiment Sentiment { get; set; }
        public Urgency Urgency { get; set; }
        public InformationLevel InformationNeed { get; set; } // What client needs
        public Sentiment EmotionalState { get; set; }
        public List<string> Concerns { get; set; } = new();
        public ClientReaction? ClientReaction { get; set; }
        public Dictionary<string, double> KeywordScores { get; set; } = new();
    }

    // Response strategy determined by agent
    public class ResponseStrategy
    {
        public PreferredTone Tone { get; set; }
        public InformationLevel InformationLevel { get; set; }
        public string Approach { get; set; } = string.Empty; // Reassuring, Problem-Solving, Educational
        public List<string> SuggestedActions { get; set; } = new();
        public decimal Confidence { get; set; }
    }

    // Agentic AI response
    public class AgenticResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<string> SuggestedActions { get; set; } = new();
        public decimal Confidence { get; set; }
        public MessageAnalysis? Analysis { get; set; }
        public ResponseStrategy? Strategy { get; set; }
    }

    // Request/Response models for API
    public class ProcessServiceRequestRequest
    {
        [Required]
        public int ClientId { get; set; }
        
        [Required]
        public string ClientMessage { get; set; } = string.Empty;
        
        public int? ServiceRequestId { get; set; }
    }

    public class ProcessServiceRequestResponse
    {
        public bool Success { get; set; }
        public AgenticResponse? Response { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

