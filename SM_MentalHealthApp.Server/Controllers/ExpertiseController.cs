using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpertiseController : BaseController
    {
        private readonly IExpertiseService _expertiseService;
        private readonly ILogger<ExpertiseController> _logger;

        public ExpertiseController(IExpertiseService expertiseService, ILogger<ExpertiseController> logger)
        {
            _expertiseService = expertiseService;
            _logger = logger;
        }

        /// <summary>
        /// Get all expertise categories
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Coordinator,Doctor,Attorney,SME")]
        public async Task<ActionResult<List<Expertise>>> GetAllExpertises([FromQuery] bool activeOnly = true)
        {
            try
            {
                var expertises = await _expertiseService.GetAllExpertisesAsync(activeOnly);
                return Ok(expertises);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expertises");
                return StatusCode(500, "An error occurred while getting expertises");
            }
        }

        /// <summary>
        /// Get expertise by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<Expertise>> GetExpertise(int id)
        {
            try
            {
                var expertise = await _expertiseService.GetExpertiseByIdAsync(id);
                if (expertise == null)
                    return NotFound();

                return Ok(expertise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expertise {Id}", id);
                return StatusCode(500, "An error occurred while getting expertise");
            }
        }

        /// <summary>
        /// Create a new expertise category
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<Expertise>> CreateExpertise([FromBody] CreateExpertiseRequest request)
        {
            try
            {
                var expertise = await _expertiseService.CreateExpertiseAsync(request.Name, request.Description);
                return CreatedAtAction(nameof(GetExpertise), new { id = expertise.Id }, expertise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expertise");
                return StatusCode(500, "An error occurred while creating expertise");
            }
        }

        /// <summary>
        /// Update an expertise category
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<Expertise>> UpdateExpertise(int id, [FromBody] UpdateExpertiseRequest request)
        {
            try
            {
                var expertise = await _expertiseService.UpdateExpertiseAsync(id, request.Name, request.Description, request.IsActive);
                if (expertise == null)
                    return NotFound();

                return Ok(expertise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expertise {Id}", id);
                return StatusCode(500, "An error occurred while updating expertise");
            }
        }

        /// <summary>
        /// Delete (soft delete) an expertise category
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteExpertise(int id)
        {
            try
            {
                var result = await _expertiseService.DeleteExpertiseAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expertise {Id}", id);
                return StatusCode(500, "An error occurred while deleting expertise");
            }
        }

        /// <summary>
        /// Get expertise IDs for an SME
        /// </summary>
        [HttpGet("sme/{smeUserId}")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<List<int>>> GetSmeExpertises(int smeUserId)
        {
            try
            {
                var expertiseIds = await _expertiseService.GetExpertiseIdsForSmeAsync(smeUserId);
                return Ok(expertiseIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SME expertises for SME {SmeUserId}", smeUserId);
                return StatusCode(500, "An error occurred while getting SME expertises");
            }
        }

        /// <summary>
        /// Set expertise for an SME
        /// </summary>
        [HttpPost("sme/{smeUserId}")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult> SetSmeExpertises(int smeUserId, [FromBody] SetSmeExpertisesRequest request)
        {
            try
            {
                var result = await _expertiseService.SetSmeExpertisesAsync(smeUserId, request.ExpertiseIds);
                if (!result)
                    return BadRequest("Failed to set SME expertises");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting SME expertises for SME {SmeUserId}", smeUserId);
                return StatusCode(500, "An error occurred while setting SME expertises");
            }
        }

        /// <summary>
        /// Get expertise IDs for a Service Request
        /// </summary>
        [HttpGet("service-request/{serviceRequestId}")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<List<int>>> GetServiceRequestExpertises(int serviceRequestId)
        {
            try
            {
                var expertiseIds = await _expertiseService.GetExpertiseIdsForServiceRequestAsync(serviceRequestId);
                return Ok(expertiseIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ServiceRequest expertises for SR {ServiceRequestId}", serviceRequestId);
                return StatusCode(500, "An error occurred while getting ServiceRequest expertises");
            }
        }
    }

    // Request models
    public class CreateExpertiseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateExpertiseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }

    public class SetSmeExpertisesRequest
    {
        public List<int> ExpertiseIds { get; set; } = new();
    }
}

