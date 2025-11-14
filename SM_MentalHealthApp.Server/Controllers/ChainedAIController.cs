using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor,Admin")]
    public class ChainedAIController : BaseController
    {
        private readonly IChainedAIService _chainedAIService;
        private readonly ILogger<ChainedAIController> _logger;
        private readonly JournalDbContext _context;

        public ChainedAIController(
            IChainedAIService chainedAIService,
            ILogger<ChainedAIController> logger,
            JournalDbContext context)
        {
            _chainedAIService = chainedAIService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Generate structured clinical note and analysis using chained AI (BioMistral + Meditron)
        /// </summary>
        [HttpPost("generate-note-and-analysis")]
        public async Task<ActionResult<ChainedAIResult>> GenerateNoteAndAnalysis(
            [FromBody] ChainedAIRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.EncounterData))
                {
                    return BadRequest("Encounter data is required");
                }

                if (request.PatientId <= 0)
                {
                    return BadRequest("Valid patient ID is required");
                }

                var result = await _chainedAIService.GenerateStructuredNoteAndAnalysisAsync(
                    request.EncounterData,
                    request.PatientId);

                if (!result.Success)
                {
                    return StatusCode(500, new { error = result.ErrorMessage });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating chained AI note and analysis");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get current chained AI configuration (for verification)
        /// </summary>
        [HttpGet("current-config")]
        public async Task<ActionResult> GetCurrentConfig([FromQuery] string context = "ClinicalNote")
        {
            try
            {
                var chain = await _context.Set<AIModelChain>()
                    .Include(c => c.PrimaryModel)
                    .Include(c => c.SecondaryModel)
                    .Where(c => c.Context == context && c.IsActive)
                    .OrderBy(c => c.ChainOrder)
                    .FirstOrDefaultAsync();

                if (chain == null)
                {
                    return NotFound(new { 
                        message = "No active chain configuration found",
                        context = context,
                        suggestion = "Please run the SQL migration and seed scripts"
                    });
                }

                return Ok(new
                {
                    ChainName = chain.ChainName,
                    Context = chain.Context,
                    Description = chain.Description,
                    PrimaryModel = new
                    {
                        Id = chain.PrimaryModel.Id,
                        Name = chain.PrimaryModel.ModelName,
                        Type = chain.PrimaryModel.ModelType,
                        Provider = chain.PrimaryModel.Provider,
                        Endpoint = chain.PrimaryModel.ApiEndpoint,
                        IsActive = chain.PrimaryModel.IsActive
                    },
                    SecondaryModel = new
                    {
                        Id = chain.SecondaryModel.Id,
                        Name = chain.SecondaryModel.ModelName,
                        Type = chain.SecondaryModel.ModelType,
                        Provider = chain.SecondaryModel.Provider,
                        Endpoint = chain.SecondaryModel.ApiEndpoint,
                        IsActive = chain.SecondaryModel.IsActive
                    },
                    ChainOrder = chain.ChainOrder,
                    IsActive = chain.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current config");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }
    }

    public class ChainedAIRequest
    {
        public string EncounterData { get; set; } = string.Empty;
        public int PatientId { get; set; }
    }
}

