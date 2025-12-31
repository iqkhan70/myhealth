using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Shared.Constants;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoicingController : BaseController
    {
        private readonly IInvoicingService _invoicingService;
        private readonly ILogger<InvoicingController> _logger;

        public InvoicingController(
            IInvoicingService invoicingService,
            ILogger<InvoicingController> logger)
        {
            _invoicingService = invoicingService;
            _logger = logger;
        }

        /// <summary>
        /// Generate an invoice for an SME
        /// </summary>
        [HttpPost("generate")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<SmeInvoiceDto>> GenerateInvoice([FromBody] GenerateInvoiceRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var invoice = await _invoicingService.GenerateInvoiceAsync(request, currentUserId);
                return Ok(invoice);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot generate invoice: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice");
                return StatusCode(500, "An error occurred while generating the invoice");
            }
        }

        /// <summary>
        /// Mark an invoice as paid
        /// </summary>
        [HttpPost("{invoiceId}/mark-paid")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> MarkInvoicePaid(long invoiceId, [FromBody] MarkInvoicePaidRequest? request = null)
        {
            try
            {
                var paid = await _invoicingService.MarkInvoicePaidAsync(
                    invoiceId, 
                    request?.PaidDate, 
                    request?.PaymentNotes);

                if (!paid)
                    return BadRequest("Failed to mark invoice as paid. Invoice may not exist or is already paid.");

                return Ok(new { message = "Invoice marked as paid successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice {InvoiceId} as paid", invoiceId);
                return StatusCode(500, "An error occurred while marking the invoice as paid");
            }
        }

        /// <summary>
        /// Void an invoice
        /// </summary>
        [HttpPost("{invoiceId}/void")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> VoidInvoice(long invoiceId, [FromBody] VoidInvoiceRequest request)
        {
            try
            {
                if (request.InvoiceId != invoiceId)
                    return BadRequest("Invoice ID mismatch");

                var voided = await _invoicingService.VoidInvoiceAsync(
                    invoiceId, 
                    request.Reason, 
                    request.ResetAssignmentsToReady);

                if (!voided)
                    return BadRequest("Failed to void invoice. Invoice may not exist.");

                return Ok(new { message = "Invoice voided successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voiding invoice {InvoiceId}", invoiceId);
                return StatusCode(500, "An error occurred while voiding the invoice");
            }
        }

        /// <summary>
        /// Get invoices with optional filters
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<List<SmeInvoiceDto>>> GetInvoices(
            [FromQuery] int? smeUserId = null,
            [FromQuery] InvoiceStatus? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var invoices = await _invoicingService.GetInvoicesAsync(smeUserId, status, startDate, endDate);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoices");
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        /// <summary>
        /// Get invoice by ID
        /// </summary>
        [HttpGet("{invoiceId}")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<SmeInvoiceDto>> GetInvoice(long invoiceId)
        {
            try
            {
                var invoice = await _invoicingService.GetInvoiceByIdAsync(invoiceId);
                if (invoice == null)
                    return NotFound();

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice {InvoiceId}", invoiceId);
                return StatusCode(500, "An error occurred while retrieving the invoice");
            }
        }

        /// <summary>
        /// Get assignments ready to be billed (for invoice generation preview)
        /// </summary>
        [HttpGet("ready-to-bill/{smeUserId}")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<List<BillableAssignmentDto>>> GetReadyToBillAssignments(
            int smeUserId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var assignments = await _invoicingService.GetReadyToBillAssignmentsAsync(smeUserId, startDate, endDate);
                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ready-to-bill assignments for SME {SmeUserId}", smeUserId);
                return StatusCode(500, "An error occurred while retrieving ready-to-bill assignments");
            }
        }
    }
}

