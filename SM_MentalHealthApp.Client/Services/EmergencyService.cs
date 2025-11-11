using SM_MentalHealthApp.Shared;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SM_MentalHealthApp.Client.Services
{
    public class EmergencyService : BaseService, IEmergencyService
    {
        public EmergencyService(HttpClient http, IAuthService authService) : base(http, authService)
        {
        }

        public async Task<IEnumerable<EmergencyAlert>> ListAsync(int? doctorId, CancellationToken ct = default)
        {
            AddAuthorizationHeader();
            var url = doctorId.HasValue
                ? $"api/emergency/incidents/{doctorId.Value}"
                : "api/emergency/incidents";
            return await _http.GetFromJsonAsync<IEnumerable<EmergencyAlert>>(url, ct) ?? new List<EmergencyAlert>();
        }

        public async Task<EmergencyAlert?> GetAsync(int incidentId, CancellationToken ct = default)
        {
            AddAuthorizationHeader();
            return await _http.GetFromJsonAsync<EmergencyAlert>($"api/emergency/incident/{incidentId}", ct);
        }

        public async Task<bool> AcknowledgeAsync(int incidentId, int doctorId, string response, string actionTaken, CancellationToken ct = default)
        {
            AddAuthorizationHeader();
            var request = new
            {
                DoctorId = doctorId,
                Response = response,
                ActionTaken = actionTaken
            };
            var httpResponse = await _http.PostAsJsonAsync($"api/emergency/acknowledge/{incidentId}", request, ct);
            return httpResponse.IsSuccessStatusCode;
        }
    }
}

