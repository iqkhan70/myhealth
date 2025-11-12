using SM_MentalHealthApp.Shared;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SM_MentalHealthApp.Client.Services
{
    public interface IEmergencyService
    {
        Task<IEnumerable<EmergencyAlert>> ListAsync(int? doctorId, CancellationToken ct = default);
        Task<EmergencyAlert?> GetAsync(int incidentId, CancellationToken ct = default);
        Task<bool> AcknowledgeAsync(int incidentId, int doctorId, string response, string actionTaken, CancellationToken ct = default);
        Task<bool> UnacknowledgeAsync(int incidentId, CancellationToken ct = default);
    }
}

