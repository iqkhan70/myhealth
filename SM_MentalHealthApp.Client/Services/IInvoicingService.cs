using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services;

public interface IInvoicingService
{
    Task<SmeInvoiceDto> GenerateInvoiceAsync(GenerateInvoiceRequest request);
    Task<bool> MarkInvoicePaidAsync(long invoiceId, DateTime? paidDate = null, string? paymentNotes = null);
    Task<bool> VoidInvoiceAsync(long invoiceId, string reason, bool resetAssignmentsToReady = true);
    Task<List<SmeInvoiceDto>> GetInvoicesAsync(int? smeUserId = null, InvoiceStatus? status = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<SmeInvoiceDto?> GetInvoiceByIdAsync(long invoiceId);
    Task<List<BillableAssignmentDto>> GetReadyToBillAssignmentsAsync(int smeUserId, DateTime? startDate = null, DateTime? endDate = null);
}

