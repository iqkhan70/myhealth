using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Configuration for AI models (BioMistral, Meditron, etc.)
    /// </summary>
    public class AIModelConfig
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ModelName { get; set; } = string.Empty; // e.g., "BioMistral", "Meditron"
        
        [Required]
        [MaxLength(50)]
        public string ModelType { get; set; } = string.Empty; // "Primary", "Secondary", "Chained"
        
        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty; // "HuggingFace", "OpenAI", "Ollama", etc.
        
        [Required]
        [MaxLength(500)]
        public string ApiEndpoint { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? ApiKeyConfigKey { get; set; } // Configuration key for API key (e.g., "HuggingFace:ApiKey")
        
        public string? SystemPrompt { get; set; } // System prompt/instructions for the model
        
        [Required]
        [MaxLength(50)]
        public string Context { get; set; } = "ClinicalNote"; // "ClinicalNote", "HealthCheck", "Chat", etc.
        
        public int DisplayOrder { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }
    
    /// <summary>
    /// Configuration for chained AI models (e.g., BioMistral -> Meditron)
    /// </summary>
    public class AIModelChain
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ChainName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Context { get; set; } = "ClinicalNote";
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public int PrimaryModelId { get; set; } // First model in chain (e.g., BioMistral)
        
        [Required]
        public int SecondaryModelId { get; set; } // Second model in chain (e.g., Meditron)
        
        public int ChainOrder { get; set; } = 1; // Order of execution
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public AIModelConfig? PrimaryModel { get; set; }
        public AIModelConfig? SecondaryModel { get; set; }
    }
    
    /// <summary>
    /// Result from chained AI processing
    /// </summary>
    public class ChainedAIResult
    {
        public string PrimaryModelOutput { get; set; } = string.Empty; // BioMistral output (structured note)
        public string SecondaryModelOutput { get; set; } = string.Empty; // Meditron output (missed considerations)
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string PrimaryModelName { get; set; } = string.Empty;
        public string SecondaryModelName { get; set; } = string.Empty;
    }
}

