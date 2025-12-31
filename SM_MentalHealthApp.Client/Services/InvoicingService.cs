using SM_MentalHealthApp.Shared;
using System.Net.Http.Json;

namespace SM_MentalHealthApp.Client.Services;

public class InvoicingService : BaseService, IInvoicingService
{
    public InvoicingService(HttpClient http, IAuthService authService) : base(http, authService)
    {
    }

    public async Task<SmeInvoiceDto> GenerateInvoiceAsync(GenerateInvoiceRequest request)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsJsonAsync("api/Invoicing/generate", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SmeInvoiceDto>() ?? throw new Exception("Failed to generate invoice");
    }

    public async Task<bool> MarkInvoicePaidAsync(long invoiceId, DateTime? paidDate = null, string? paymentNotes = null)
    {
        AddAuthorizationHeader();
        var request = new MarkInvoicePaidRequest
        {
            InvoiceId = invoiceId,
            PaidDate = paidDate,
            PaymentNotes = paymentNotes
        };
        var response = await _http.PostAsJsonAsync($"api/Invoicing/{invoiceId}/mark-paid", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> VoidInvoiceAsync(long invoiceId, string reason, bool resetAssignmentsToReady = true)
    {
        AddAuthorizationHeader();
        var request = new VoidInvoiceRequest
        {
            InvoiceId = invoiceId,
            Reason = reason,
            ResetAssignmentsToReady = resetAssignmentsToReady
        };
        var response = await _http.PostAsJsonAsync($"api/Invoicing/{invoiceId}/void", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<SmeInvoiceDto>> GetInvoicesAsync(int? smeUserId = null, InvoiceStatus? status = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        AddAuthorizationHeader();
        var queryParams = new List<string>();
        if (smeUserId.HasValue)
            queryParams.Add($"smeUserId={smeUserId.Value}");
        if (status.HasValue)
            queryParams.Add($"status={(int)status.Value}");
        if (startDate.HasValue)
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue)
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        
        var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _http.GetFromJsonAsync<List<SmeInvoiceDto>>($"api/Invoicing{query}") ?? new List<SmeInvoiceDto>();
    }

    public async Task<SmeInvoiceDto?> GetInvoiceByIdAsync(long invoiceId)
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<SmeInvoiceDto>($"api/Invoicing/{invoiceId}");
    }

    public async Task<List<BillableAssignmentDto>> GetReadyToBillAssignmentsAsync(int smeUserId, DateTime? startDate = null, DateTime? endDate = null)
    {
        AddAuthorizationHeader();
        var queryParams = new List<string>();
        if (startDate.HasValue)
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue)
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        
        var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _http.GetFromJsonAsync<List<BillableAssignmentDto>>($"api/Invoicing/ready-to-bill/{smeUserId}{query}") ?? new List<BillableAssignmentDto>();
    }
}

