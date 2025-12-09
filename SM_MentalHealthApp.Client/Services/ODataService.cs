using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SM_MentalHealthApp.Shared;
using System.Linq;

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
            // Note: We don't use $expand for Appointments because navigation properties are marked [JsonIgnore]
            // Instead, we fetch users separately if navigation properties are missing
            // For Contents, navigation properties are not marked [JsonIgnore], but we'll fetch them separately if needed
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
            
            // Explicitly request JSON response (OData can return XML by default)
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/odata+json"));

            // Make request
            var response = await _http.SendAsync(request, ct);
            
            // Read response content first (before checking status) to help with debugging
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            var statusCode = response.StatusCode;
            var fullUrl = _http.BaseAddress != null ? new Uri(_http.BaseAddress, url).ToString() : url;
            
            // Log request details for debugging (especially on DigitalOcean)
            System.Diagnostics.Debug.WriteLine($"[ODataService] Request URL: {fullUrl}");
            System.Diagnostics.Debug.WriteLine($"[ODataService] Status Code: {statusCode}");
            System.Diagnostics.Debug.WriteLine($"[ODataService] Content-Type: {contentType}");
            System.Diagnostics.Debug.WriteLine($"[ODataService] Response Length: {responseContent?.Length ?? 0}");
            if (responseContent != null && responseContent.Length > 0)
            {
                var preview = responseContent.Length > 200 ? responseContent.Substring(0, 200) : responseContent;
                System.Diagnostics.Debug.WriteLine($"[ODataService] Response Preview: {preview}");
            }
            
            // Check if response is successful before parsing
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"OData query failed with status {statusCode}. URL: {fullUrl}. Response: {responseContent?.Substring(0, Math.Min(1000, responseContent?.Length ?? 0)) ?? "No content"}")
                {
                    Data = { 
                        ["StatusCode"] = statusCode, 
                        ["ErrorContent"] = responseContent, 
                        ["Url"] = fullUrl,
                        ["ContentType"] = contentType
                    }
                };
            }
            
            // Check content type to ensure we're getting JSON
            if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("json") && !contentType.Contains("odata"))
            {
                throw new HttpRequestException($"OData endpoint returned unexpected content type: {contentType}. URL: {fullUrl}. Response preview: {responseContent?.Substring(0, Math.Min(500, responseContent?.Length ?? 0)) ?? "No content"}")
                {
                    Data = { 
                        ["ContentType"] = contentType, 
                        ["Url"] = fullUrl,
                        ["ResponsePreview"] = responseContent?.Substring(0, Math.Min(500, responseContent?.Length ?? 0))
                    }
                };
            }

            // Validate that we have valid JSON (not empty, not HTML)
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                throw new HttpRequestException($"OData endpoint returned empty response. URL: {fullUrl}")
                {
                    Data = { ["Url"] = fullUrl, ["StatusCode"] = statusCode, ["ContentType"] = contentType }
                };
            }
            
            // Check if response looks like HTML (error page) - common on DigitalOcean with nginx
            var trimmedContent = responseContent.TrimStart();
            if (trimmedContent.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) || 
                trimmedContent.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
                trimmedContent.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                // This is likely an nginx error page or XML response
                var isXml = trimmedContent.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase);
                var errorType = isXml ? "XML" : "HTML";
                throw new HttpRequestException($"OData endpoint returned {errorType} instead of JSON. This might indicate a routing issue, nginx misconfiguration, or the endpoint doesn't exist. URL: {fullUrl}. Response preview: {responseContent.Substring(0, Math.Min(1000, responseContent.Length))}")
                {
                    Data = { 
                        ["Url"] = fullUrl, 
                        ["ResponsePreview"] = responseContent.Substring(0, Math.Min(1000, responseContent.Length)),
                        ["ContentType"] = contentType,
                        ["StatusCode"] = statusCode,
                        ["IsXml"] = isXml
                    }
                };
            }
            
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(responseContent);
            }
            catch (JsonException ex)
            {
                // Enhanced error message with more context
                var preview = responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent;
                throw new HttpRequestException($"Failed to parse OData JSON response. URL: {fullUrl}. Status: {statusCode}. Content-Type: {contentType}. Response preview: {preview}. JSON Error: {ex.Message}", ex)
                {
                    Data = { 
                        ["Url"] = fullUrl, 
                        ["ResponsePreview"] = preview,
                        ["StatusCode"] = statusCode,
                        ["ContentType"] = contentType,
                        ["JsonError"] = ex.Message
                    }
                };
            }
            var root = doc.RootElement;
            
            // Additional validation: ensure root has expected OData structure
            if (!root.TryGetProperty("value", out _) && !root.TryGetProperty("@odata.context", out _))
            {
                var rootJson = System.Text.Json.JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
                throw new HttpRequestException($"OData response doesn't have expected structure (missing 'value' or '@odata.context'). URL: {fullUrl}. Root element: {rootJson.Substring(0, Math.Min(500, rootJson.Length))}")
                {
                    Data = { ["Url"] = fullUrl, ["RootElement"] = rootJson }
                };
            }

            // Extract items from "value" array
            var items = new List<T>();
            if (root.TryGetProperty("value", out var valueArray) && valueArray.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                // Special handling for Appointment -> AppointmentDto mapping
                if (typeof(T) == typeof(Shared.AppointmentDto) && entitySet == "Appointments")
                {
                    // Parse each appointment individually to handle TimeSpan conversion
                    var appointments = new List<Shared.Appointment>();
                    foreach (var appointmentElement in valueArray.EnumerateArray())
                    {
                        try
                        {
                            var appointment = new Shared.Appointment();
                            
                            // Manually deserialize each property to handle TimeSpan
                            if (appointmentElement.TryGetProperty("Id", out var idElement))
                                appointment.Id = idElement.GetInt32();
                            if (appointmentElement.TryGetProperty("DoctorId", out var doctorIdElement))
                                appointment.DoctorId = doctorIdElement.GetInt32();
                            if (appointmentElement.TryGetProperty("PatientId", out var patientIdElement))
                                appointment.PatientId = patientIdElement.GetInt32();
                            if (appointmentElement.TryGetProperty("AppointmentDateTime", out var dateTimeElement))
                                appointment.AppointmentDateTime = dateTimeElement.GetDateTime();
                            if (appointmentElement.TryGetProperty("Duration", out var durationElement))
                            {
                                // OData serializes TimeSpan as string (e.g., "00:30:00")
                                if (durationElement.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var durationStr = durationElement.GetString();
                                    if (!string.IsNullOrEmpty(durationStr) && TimeSpan.TryParse(durationStr, out var duration))
                                    {
                                        appointment.Duration = duration;
                                    }
                                }
                            }
                            if (appointmentElement.TryGetProperty("AppointmentType", out var typeElement))
                            {
                                if (typeElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                                    appointment.AppointmentType = (Shared.AppointmentType)typeElement.GetInt32();
                                else if (typeElement.ValueKind == System.Text.Json.JsonValueKind.String && Enum.TryParse<Shared.AppointmentType>(typeElement.GetString(), out var parsedType))
                                    appointment.AppointmentType = parsedType;
                            }
                            if (appointmentElement.TryGetProperty("Status", out var statusElement))
                            {
                                if (statusElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                                    appointment.Status = (Shared.AppointmentStatus)statusElement.GetInt32();
                                else if (statusElement.ValueKind == System.Text.Json.JsonValueKind.String && Enum.TryParse<Shared.AppointmentStatus>(statusElement.GetString(), out var parsedStatus))
                                    appointment.Status = parsedStatus;
                            }
                            if (appointmentElement.TryGetProperty("Reason", out var reasonElement))
                                appointment.Reason = reasonElement.GetString();
                            if (appointmentElement.TryGetProperty("Notes", out var notesElement))
                                appointment.Notes = notesElement.GetString();
                            if (appointmentElement.TryGetProperty("CreatedByUserId", out var createdByElement))
                                appointment.CreatedByUserId = createdByElement.GetInt32();
                            if (appointmentElement.TryGetProperty("CreatedAt", out var createdAtElement))
                                appointment.CreatedAt = createdAtElement.GetDateTime();
                            if (appointmentElement.TryGetProperty("UpdatedAt", out var updatedAtElement) && updatedAtElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                                appointment.UpdatedAt = updatedAtElement.GetDateTime();
                            if (appointmentElement.TryGetProperty("IsActive", out var isActiveElement))
                                appointment.IsActive = isActiveElement.GetBoolean();
                            if (appointmentElement.TryGetProperty("IsBusinessHours", out var isBusinessHoursElement))
                                appointment.IsBusinessHours = isBusinessHoursElement.GetBoolean();
                            if (appointmentElement.TryGetProperty("TimeZoneId", out var timeZoneElement))
                                appointment.TimeZoneId = timeZoneElement.GetString() ?? "UTC";
                            
                            // Note: Navigation properties (Doctor, Patient, CreatedByUser) are marked [JsonIgnore]
                            // so they won't be in the OData response. We'll fetch them separately below.
                            
                            appointments.Add(appointment);
                        }
                        catch (Exception ex)
                        {
                            // Continue with next appointment
                        }
                    }
                    
                    // Fetch users for all appointments (navigation properties are marked [JsonIgnore] so they won't be in OData response)
                    var doctorIds = appointments.Select(a => a.DoctorId).Distinct().ToList();
                    var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();
                    var createdByUserIds = appointments.Select(a => a.CreatedByUserId).Distinct().ToList();
                    
                    var allUserIds = doctorIds.Concat(patientIds).Concat(createdByUserIds).Distinct().ToList();
                    
                    // Fetch users via OData if needed
                    var userCache = new Dictionary<int, Shared.User>();
                    if (allUserIds.Any())
                    {
                        try
                        {
                            // Build filter for multiple user IDs: Id eq 1 or Id eq 2 or Id eq 3
                            var userIdFilters = allUserIds.Select(id => $"Id eq {id}");
                            var userFilter = string.Join(" or ", userIdFilters);
                            
                            // Query users via OData
                            var userRequest = new HttpRequestMessage(HttpMethod.Get, $"odata/Users?$filter={Uri.EscapeDataString(userFilter)}");
                            var userToken = _authService.Token;
                            if (!string.IsNullOrEmpty(userToken))
                            {
                                userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
                            }
                            
                            var userResponse = await _http.SendAsync(userRequest, ct);
                            if (userResponse.IsSuccessStatusCode)
                            {
                                var userJson = await userResponse.Content.ReadAsStringAsync(ct);
                                var userDoc = JsonDocument.Parse(userJson);
                                if (userDoc.RootElement.TryGetProperty("value", out var userArray) && userArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    var users = JsonSerializer.Deserialize<List<Shared.User>>(userArray.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Shared.User>();
                                    foreach (var user in users)
                                    {
                                        userCache[user.Id] = user;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    
                    // Populate navigation properties from cache (they're always null from OData due to [JsonIgnore])
                    foreach (var appointment in appointments)
                    {
                        if (userCache.TryGetValue(appointment.DoctorId, out var doctor))
                            appointment.Doctor = doctor;
                        if (userCache.TryGetValue(appointment.PatientId, out var patient))
                            appointment.Patient = patient;
                        if (userCache.TryGetValue(appointment.CreatedByUserId, out var createdBy))
                            appointment.CreatedByUser = createdBy;
                    }
                    
                    // Map to DTOs
                    items = appointments.Select(a => MapAppointmentToDto(a)).Cast<T>().ToList();
                }
                // Special handling for UserAssignment (navigation properties are marked [JsonIgnore])
                else if (typeof(T) == typeof(Shared.UserAssignment) && entitySet == "UserAssignments")
                {
                    // Parse each UserAssignment individually
                    var userAssignments = new List<Shared.UserAssignment>();
                    foreach (var assignmentElement in valueArray.EnumerateArray())
                    {
                        try
                        {
                            var assignment = new Shared.UserAssignment();
                            
                            // Manually deserialize each property
                            if (assignmentElement.TryGetProperty("AssignerId", out var assignerIdElement))
                                assignment.AssignerId = assignerIdElement.GetInt32();
                            if (assignmentElement.TryGetProperty("AssigneeId", out var assigneeIdElement))
                                assignment.AssigneeId = assigneeIdElement.GetInt32();
                            if (assignmentElement.TryGetProperty("AssignedAt", out var assignedAtElement))
                                assignment.AssignedAt = assignedAtElement.GetDateTime();
                            if (assignmentElement.TryGetProperty("IsActive", out var isActiveElement))
                                assignment.IsActive = isActiveElement.GetBoolean();
                            
                            // Check if navigation properties are in the response (they might be even with [JsonIgnore] if OData expands them)
                            if (assignmentElement.TryGetProperty("Assigner", out var assignerElement) && assignerElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                var assigner = JsonSerializer.Deserialize<Shared.User>(assignerElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (assigner != null)
                                    assignment.Assigner = assigner;
                            }
                            if (assignmentElement.TryGetProperty("Assignee", out var assigneeElement) && assigneeElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                var assignee = JsonSerializer.Deserialize<Shared.User>(assigneeElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (assignee != null)
                                    assignment.Assignee = assignee;
                            }
                            
                            // Note: Navigation properties (Assigner, Assignee) are marked [JsonIgnore]
                            // but OData might still include them if $expand is used. If not, we'll fetch them separately below.
                            
                            userAssignments.Add(assignment);
                        }
                        catch (Exception ex)
                        {
                            // Continue with next assignment
                        }
                    }
                    
                    // Fetch users for all assignments that don't already have navigation properties populated
                    var assignerIds = userAssignments.Where(ua => ua.Assigner == null).Select(ua => ua.AssignerId).Distinct().ToList();
                    var assigneeIds = userAssignments.Where(ua => ua.Assignee == null).Select(ua => ua.AssigneeId).Distinct().ToList();
                    
                    var allUserIds = assignerIds.Concat(assigneeIds).Distinct().ToList();
                    
                    // Fetch users via OData if needed
                    var userCache = new Dictionary<int, Shared.User>();
                    if (allUserIds.Any())
                    {
                        try
                        {
                            // Build filter for multiple user IDs: Id eq 1 or Id eq 2 or Id eq 3
                            var userIdFilters = allUserIds.Select(id => $"Id eq {id}");
                            var userFilter = string.Join(" or ", userIdFilters);
                            
                            // Query users via OData
                            var userRequest = new HttpRequestMessage(HttpMethod.Get, $"odata/Users?$filter={Uri.EscapeDataString(userFilter)}");
                            var userToken = _authService.Token;
                            if (!string.IsNullOrEmpty(userToken))
                            {
                                userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
                            }
                            
                            var userResponse = await _http.SendAsync(userRequest, ct);
                            if (userResponse.IsSuccessStatusCode)
                            {
                                var userJson = await userResponse.Content.ReadAsStringAsync(ct);
                                var userDoc = JsonDocument.Parse(userJson);
                                if (userDoc.RootElement.TryGetProperty("value", out var userArray) && userArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    var users = JsonSerializer.Deserialize<List<Shared.User>>(userArray.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Shared.User>();
                                    foreach (var user in users)
                                    {
                                        userCache[user.Id] = user;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    
                    // Populate navigation properties from cache (only if they weren't already populated from OData response)
                    foreach (var assignment in userAssignments)
                    {
                        if (assignment.Assigner == null && userCache.TryGetValue(assignment.AssignerId, out var assigner))
                            assignment.Assigner = assigner;
                        if (assignment.Assignee == null && userCache.TryGetValue(assignment.AssigneeId, out var assignee))
                            assignment.Assignee = assignee;
                    }
                    
                    // Return UserAssignments with populated navigation properties
                    items = userAssignments.Cast<T>().ToList();
                }
                else
                {
                    items = JsonSerializer.Deserialize<List<T>>(valueArray.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<T>();
                }
            }

            // Extract total count from "@odata.count"
            var totalCount = 0;
            if (root.TryGetProperty("@odata.count", out var countElement))
            {
                totalCount = countElement.GetInt32();
            }
            else if (root.TryGetProperty("odata.count", out var countElement2))
            {
                // Try lowercase version
                totalCount = countElement2.GetInt32();
            }
            else
            {
                // Fallback: if no count, use items count (but this might be wrong for pagination)
                // For appointments, we should always have a count
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

    private static AppointmentDto MapAppointmentToDto(Shared.Appointment appointment)
    {
        return new AppointmentDto
        {
            Id = appointment.Id,
            DoctorId = appointment.DoctorId,
            DoctorName = appointment.Doctor != null
                ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
                : "Unknown Doctor",
            DoctorEmail = appointment.Doctor?.Email ?? "",
            PatientId = appointment.PatientId,
            PatientName = appointment.Patient != null
                ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}"
                : "Unknown Patient",
            PatientEmail = appointment.Patient?.Email ?? "",
            AppointmentDateTime = appointment.AppointmentDateTime,
            EndDateTime = appointment.EndDateTime,
            Duration = appointment.Duration,
            AppointmentType = appointment.AppointmentType,
            Status = appointment.Status,
            Reason = appointment.Reason,
            Notes = appointment.Notes,
            IsUrgentCare = appointment.IsUrgentCare,
            IsBusinessHours = appointment.IsBusinessHours,
            TimeZoneId = appointment.TimeZoneId,
            CreatedBy = appointment.CreatedByUser != null
                ? $"{appointment.CreatedByUser.FirstName} {appointment.CreatedByUser.LastName}"
                : "Unknown",
            CreatedAt = appointment.CreatedAt
        };
    }
}

