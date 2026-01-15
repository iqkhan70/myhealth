using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Tracks client agent conversation state and active Service Request context
    /// Ensures every agent conversation is tied to an SR (SR-first approach)
    /// </summary>
    public class ClientAgentSession
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        /// <summary>
        /// Active SR context for this conversation. All messages are tied to this SR.
        /// </summary>
        public int? CurrentServiceRequestId { get; set; }

        /// <summary>
        /// Current conversation state:
        /// - NoActiveSRContext: Client is chatting, but no SR context yet
        /// - SelectingExistingSR: Agent is asking which existing SR this relates to
        /// - CreatingNewSR: Agent is gathering info to create a new SR
        /// - InSRContext: All messages are tied to CurrentServiceRequestId
        /// </summary>
        [Required]
        [StringLength(50)]
        public string State { get; set; } = "NoActiveSRContext";

        /// <summary>
        /// SR ID that was just created, waiting for client confirmation
        /// </summary>
        public int? PendingCreatedServiceRequestId { get; set; }

        /// <summary>
        /// Structured JSON metadata for confirmations and conversation context.
        /// Stores expert system data like: {"appointmentConfirmed": true, "timeWindow": "2026-01-16 10:00-12:00", "lastConfirmedAt": "2026-01-15T10:30:00Z"}
        /// This enables: detect confirmations, avoid re-asking, optimize token costs with short answers
        /// </summary>
        public string? Metadata { get; set; }

        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? Client { get; set; }
        public ServiceRequest? CurrentServiceRequest { get; set; }
        public ServiceRequest? PendingCreatedServiceRequest { get; set; }
    }

    /// <summary>
    /// Conversation states for client agent sessions
    /// </summary>
    public enum ClientAgentSessionState
    {
        NoActiveSRContext,      // Client is chatting, but you don't know which SR it belongs to yet
        SelectingExistingSR,    // Agent is showing / asking which SR this relates to
        CreatingNewSR,          // Agent gathers minimal SR fields, creates SR via API, then confirms it
        InSRContext             // All messages are tied to CurrentServiceRequestId
    }

    /// <summary>
    /// DTO for client agent session operations
    /// </summary>
    public class ClientAgentSessionDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int? CurrentServiceRequestId { get; set; }
        public string State { get; set; } = "NoActiveSRContext";
        public int? PendingCreatedServiceRequestId { get; set; }
        public DateTime LastUpdatedUtc { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

