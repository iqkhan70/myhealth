using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    /// <summary>
    /// Service to manage client agent conversation state and SR context
    /// </summary>
    public interface IClientAgentSessionService
    {
        /// <summary>
        /// Get or create a client agent session
        /// </summary>
        Task<ClientAgentSession> GetOrCreateSessionAsync(int clientId);

        /// <summary>
        /// Get current session for a client
        /// </summary>
        Task<ClientAgentSession?> GetSessionAsync(int clientId);

        /// <summary>
        /// Set the active Service Request for a client's agent session
        /// </summary>
        Task<bool> SetActiveServiceRequestAsync(int clientId, int serviceRequestId);

        /// <summary>
        /// Clear the active Service Request (reset to NoActiveSRContext)
        /// </summary>
        Task<bool> ClearActiveServiceRequestAsync(int clientId);

        /// <summary>
        /// Update session state
        /// </summary>
        Task<bool> UpdateSessionStateAsync(int clientId, ClientAgentSessionState state, int? pendingCreatedServiceRequestId = null);

        /// <summary>
        /// Confirm a newly created SR and set it as active
        /// </summary>
        Task<bool> ConfirmCreatedServiceRequestAsync(int clientId, int serviceRequestId);

        /// <summary>
        /// Reset session to NoActiveSRContext state (useful for debugging stuck sessions)
        /// </summary>
        Task<bool> ResetSessionAsync(int clientId);

        /// <summary>
        /// Update session metadata with structured confirmation data
        /// </summary>
        Task<bool> UpdateMetadataAsync(int clientId, string metadataJson);
    }
}

