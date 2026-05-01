using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public class ClientAgentSessionService : IClientAgentSessionService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<ClientAgentSessionService> _logger;

        public ClientAgentSessionService(JournalDbContext context, ILogger<ClientAgentSessionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ClientAgentSession> GetOrCreateSessionAsync(int clientId)
        {
            // CRITICAL: Always reload from database to avoid stale Entity Framework cache
            // This ensures we get the latest state, especially if another request updated it
            // First check if entity is already tracked in this DbContext
            var trackedSession = _context.ChangeTracker.Entries<ClientAgentSession>()
                .FirstOrDefault(e => e.Entity.ClientId == clientId)?.Entity;

            if (trackedSession != null)
            {
                // Entity is tracked - reload from database to get latest state
                await _context.Entry(trackedSession).ReloadAsync();
                _logger.LogDebug("Reloaded tracked ClientAgentSession for client {ClientId} from database", clientId);
                return trackedSession;
            }

            // Not tracked - query fresh from database
            var session = await _context.ClientAgentSessions
                .FirstOrDefaultAsync(s => s.ClientId == clientId);

            if (session == null)
            {
                session = new ClientAgentSession
                {
                    ClientId = clientId,
                    State = ClientAgentSessionState.NoActiveSRContext.ToString(),
                    LastUpdatedUtc = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ClientAgentSessions.Add(session);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new ClientAgentSession for client {ClientId}", clientId);
            }
            else
            {
                // Reload to ensure we have the absolute latest state from database
                // This prevents any potential stale data issues
                await _context.Entry(session).ReloadAsync();
                _logger.LogDebug("Reloaded ClientAgentSession for client {ClientId} from database", clientId);
            }

            return session;
        }

        public async Task<ClientAgentSession?> GetSessionAsync(int clientId)
        {
            return await _context.ClientAgentSessions
                .Include(s => s.CurrentServiceRequest)
                .FirstOrDefaultAsync(s => s.ClientId == clientId);
        }

        public async Task<bool> SetActiveServiceRequestAsync(int clientId, int serviceRequestId)
        {
            try
            {
                var session = await GetOrCreateSessionAsync(clientId);
                session.CurrentServiceRequestId = serviceRequestId;
                session.State = ClientAgentSessionState.InSRContext.ToString();
                session.PendingCreatedServiceRequestId = null; // Clear any pending
                session.LastUpdatedUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Set active SR {ServiceRequestId} for client {ClientId}", serviceRequestId, clientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting active SR {ServiceRequestId} for client {ClientId}", serviceRequestId, clientId);
                return false;
            }
        }

        public async Task<bool> ClearActiveServiceRequestAsync(int clientId)
        {
            try
            {
                var session = await GetOrCreateSessionAsync(clientId);
                session.CurrentServiceRequestId = null;
                session.State = ClientAgentSessionState.NoActiveSRContext.ToString();
                session.PendingCreatedServiceRequestId = null;
                session.LastUpdatedUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleared active SR for client {ClientId}", clientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing active SR for client {ClientId}", clientId);
                return false;
            }
        }

        public async Task<bool> UpdateSessionStateAsync(int clientId, ClientAgentSessionState state, int? pendingCreatedServiceRequestId = null)
        {
            try
            {
                var session = await GetOrCreateSessionAsync(clientId);
                session.State = state.ToString();
                if (pendingCreatedServiceRequestId.HasValue)
                {
                    session.PendingCreatedServiceRequestId = pendingCreatedServiceRequestId.Value;
                }
                session.LastUpdatedUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated session state to {State} for client {ClientId}", state, clientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session state for client {ClientId}", clientId);
                return false;
            }
        }

        public async Task<bool> ConfirmCreatedServiceRequestAsync(int clientId, int serviceRequestId)
        {
            try
            {
                var session = await GetOrCreateSessionAsync(clientId);
                
                // Verify the pending SR matches
                if (session.PendingCreatedServiceRequestId == serviceRequestId)
                {
                    session.CurrentServiceRequestId = serviceRequestId;
                    session.State = ClientAgentSessionState.InSRContext.ToString();
                    session.PendingCreatedServiceRequestId = null;
                    session.LastUpdatedUtc = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Confirmed created SR {ServiceRequestId} for client {ClientId}", serviceRequestId, clientId);
                    return true;
                }
                else
                {
                    // If no pending or different SR, just set it directly
                    return await SetActiveServiceRequestAsync(clientId, serviceRequestId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming created SR {ServiceRequestId} for client {ClientId}", serviceRequestId, clientId);
                return false;
            }
        }

        public async Task<bool> ResetSessionAsync(int clientId)
        {
            try
            {
                var session = await GetOrCreateSessionAsync(clientId);
                session.CurrentServiceRequestId = null;
                session.State = ClientAgentSessionState.NoActiveSRContext.ToString();
                session.PendingCreatedServiceRequestId = null;
                session.Metadata = null; // Clear metadata on reset
                session.LastUpdatedUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Reset session to NoActiveSRContext for client {ClientId}", clientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting session for client {ClientId}", clientId);
                return false;
            }
        }

        public async Task<bool> UpdateMetadataAsync(int clientId, string metadataJson)
        {
            try
            {
                var session = await GetOrCreateSessionAsync(clientId);
                session.Metadata = metadataJson;
                session.LastUpdatedUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated metadata for client {ClientId}", clientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metadata for client {ClientId}", clientId);
                return false;
            }
        }
    }
}

