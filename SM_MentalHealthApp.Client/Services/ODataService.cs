using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services;

/// <summary>
/// Service for querying OData endpoints with server-side pagination, filtering, and sorting
/// </summary>
public class ODataService
{
    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public ODataService(HttpClient http, IAuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    /// <summary>
    /// Queries an OData endpoint and returns paginated results
    /// </summary>
    public async Task<PagedResult<T>> QueryAsync<T>(
        string entitySet,
        int skip = 0,
        int take = 20,
        string? orderBy = null,
        string? filter = null,
        CancellationToken ct = default)
    {
        try
        {
            // Build OData query
            var queryParams = new List<string>();
            if (skip > 0) queryParams.Add($"$skip={skip}");
            if (take > 0) queryParams.Add($"$top={take}");
            if (!string.IsNullOrEmpty(orderBy)) queryParams.Add($"$orderby={Uri.EscapeDataString(orderBy)}");
            if (!string.IsNullOrEmpty(filter))
            {
                // Filter is already transformed to OData syntax by the caller (e.g., Patients.razor)
                // Just URL-encode it for the query string
                queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
            }
            queryParams.Add("$count=true"); // Get total count

            var queryString = string.Join("&", queryParams);
            var url = $"odata/{entitySet}?{queryString}";

            // Create request with authorization header (don't modify DefaultRequestHeaders)
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var token = _authService.Token;
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Make request
            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            // Parse OData response
            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extract items from "value" array
            var items = new List<T>();
            if (root.TryGetProperty("value", out var valueArray) && valueArray.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                items = JsonSerializer.Deserialize<List<T>>(valueArray.GetRawText(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<T>();
            }

            // Extract total count from "@odata.count"
            var totalCount = 0;
            if (root.TryGetProperty("@odata.count", out var countElement))
            {
                totalCount = countElement.GetInt32();
            }
            else
            {
                // Fallback: if no count, use items count
                totalCount = items.Count;
            }

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = (skip / take) + 1,
                PageSize = take
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"OData query failed for {entitySet}: {ex.Message}", ex);
        }
    }
}

