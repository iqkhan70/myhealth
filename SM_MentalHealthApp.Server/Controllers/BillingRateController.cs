using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BillingRateController : ControllerBase
    {
        private readonly IBillingRateService _billingRateService;
        private readonly JournalDbContext _context;
        private readonly ILogger<BillingRateController> _logger;

        public BillingRateController(
            IBillingRateService billingRateService,
            JournalDbContext context,
            ILogger<BillingRateController> logger)
        {
            _billingRateService = billingRateService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all active billing accounts (for dropdowns)
        /// </summary>
        [HttpGet("billing-accounts")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<List<BillingAccount>>> GetBillingAccounts()
        {
            try
            {
                var accounts = await _context.BillingAccounts
                    .Include(ba => ba.Company)
                    .Include(ba => ba.User)
                    .Where(ba => ba.IsActive)
                    .OrderBy(ba => ba.Name)
                    .ThenBy(ba => ba.Type)
                    .ToListAsync();

                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing accounts");
                return StatusCode(500, $"Error getting billing accounts: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all billing rates, optionally filtered by billing account and active status
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<List<BillingRate>>> GetBillingRates(
            [FromQuery] long? billingAccountId = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var rates = await _billingRateService.GetBillingRatesAsync(billingAccountId, isActive);
                return Ok(rates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing rates");
                return StatusCode(500, $"Error getting billing rates: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a billing rate by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<BillingRate>> GetBillingRate(long id)
        {
            try
            {
                var rate = await _billingRateService.GetBillingRateByIdAsync(id);
                if (rate == null)
                {
                    return NotFound("Billing rate not found.");
                }

                return Ok(rate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing rate {Id}", id);
                return StatusCode(500, $"Error getting billing rate: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a billing rate by BillingAccountId and ExpertiseId
        /// </summary>
        [HttpGet("lookup")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<BillingRate>> GetBillingRateByAccountAndExpertise(
            [FromQuery] long billingAccountId,
            [FromQuery] int expertiseId)
        {
            try
            {
                var rate = await _billingRateService.GetBillingRateAsync(billingAccountId, expertiseId);
                if (rate == null)
                {
                    return NotFound("Billing rate not found for this billing account and expertise combination.");
                }

                return Ok(rate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing rate for BillingAccount {BillingAccountId}, Expertise {ExpertiseId}", 
                    billingAccountId, expertiseId);
                return StatusCode(500, $"Error getting billing rate: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new billing rate - Admin only
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BillingRate>> CreateBillingRate([FromBody] CreateBillingRateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var rate = await _billingRateService.CreateBillingRateAsync(request);
                
                _logger.LogInformation("BillingRate {Id} created for BillingAccount {BillingAccountId}, Expertise {ExpertiseId}, Amount {Amount}",
                    rate.Id, rate.BillingAccountId, rate.ExpertiseId, rate.Amount);

                return CreatedAtAction(nameof(GetBillingRate), new { id = rate.Id }, rate);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating billing rate");
                return StatusCode(500, $"Error creating billing rate: {ex.Message}");
            }
        }

        /// <summary>
        /// Update an existing billing rate - Admin only
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BillingRate>> UpdateBillingRate(long id, [FromBody] UpdateBillingRateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var rate = await _billingRateService.UpdateBillingRateAsync(id, request);
                if (rate == null)
                {
                    return NotFound("Billing rate not found.");
                }

                _logger.LogInformation("BillingRate {Id} updated", id);

                return Ok(rate);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating billing rate {Id}", id);
                return StatusCode(500, $"Error updating billing rate: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete (deactivate) a billing rate - Admin only
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteBillingRate(long id)
        {
            try
            {
                var deleted = await _billingRateService.DeleteBillingRateAsync(id);
                if (!deleted)
                {
                    return NotFound("Billing rate not found.");
                }

                _logger.LogInformation("BillingRate {Id} deleted (deactivated)", id);

                return Ok(new { message = "Billing rate deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting billing rate {Id}", id);
                return StatusCode(500, $"Error deleting billing rate: {ex.Message}");
            }
        }
    }
}

