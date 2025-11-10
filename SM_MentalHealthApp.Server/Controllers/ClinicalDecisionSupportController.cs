using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class ClinicalDecisionSupportController : ControllerBase
    {
        private readonly IClinicalDecisionSupportService _clinicalDecisionSupportService;
        private readonly ILogger<ClinicalDecisionSupportController> _logger;

        public ClinicalDecisionSupportController(
            IClinicalDecisionSupportService clinicalDecisionSupportService,
            ILogger<ClinicalDecisionSupportController> logger)
        {
            _clinicalDecisionSupportService = clinicalDecisionSupportService;
            _logger = logger;
        }

        /// <summary>
        /// Get comprehensive clinical recommendations for a diagnosis
        /// </summary>
        [HttpPost("recommendations")]
        public async Task<ActionResult<ClinicalRecommendation>> GetRecommendations([FromBody] ClinicalRecommendationRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int doctorId))
                {
                    return Unauthorized("Invalid user token");
                }

                _logger.LogInformation("Getting clinical recommendations for diagnosis: {Diagnosis}, Patient: {PatientId}",
                    request.Diagnosis, request.PatientId);

                var recommendations = await _clinicalDecisionSupportService.GetRecommendationsAsync(
                    request.Diagnosis,
                    request.PatientId,
                    doctorId);

                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical recommendations");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get follow-up steps for a specific diagnosis and severity
        /// </summary>
        [HttpGet("follow-up-steps")]
        public async Task<ActionResult<List<FollowUpStep>>> GetFollowUpSteps(
            [FromQuery] string diagnosis,
            [FromQuery] string severity = "Moderate")
        {
            try
            {
                var steps = await _clinicalDecisionSupportService.GetFollowUpStepsAsync(diagnosis, severity);
                return Ok(steps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting follow-up steps");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get insurance requirements for a diagnosis
        /// </summary>
        [HttpGet("insurance-requirements")]
        public async Task<ActionResult<List<InsuranceRequirement>>> GetInsuranceRequirements([FromQuery] string diagnosis)
        {
            try
            {
                var requirements = await _clinicalDecisionSupportService.GetInsuranceRequirementsAsync(diagnosis);
                return Ok(requirements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting insurance requirements");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get clinical protocol for a diagnosis
        /// </summary>
        [HttpGet("clinical-protocol")]
        public async Task<ActionResult<ClinicalProtocol>> GetClinicalProtocol([FromQuery] string diagnosis)
        {
            try
            {
                var protocol = await _clinicalDecisionSupportService.GetClinicalProtocolAsync(diagnosis);
                return Ok(protocol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical protocol");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get quick diagnosis suggestions based on symptoms
        /// </summary>
        [HttpPost("diagnosis-suggestions")]
        public async Task<ActionResult<List<DiagnosisSuggestion>>> GetDiagnosisSuggestions([FromBody] SymptomAnalysisRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int doctorId))
                {
                    return Unauthorized("Invalid user token");
                }

                var suggestions = await GetAIDiagnosisSuggestions(request.Symptoms, request.PatientId);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting diagnosis suggestions");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<List<DiagnosisSuggestion>> GetAIDiagnosisSuggestions(string symptoms, int patientId)
        {
            // This would integrate with your existing LLM service
            // For now, return some common mental health diagnoses
            return new List<DiagnosisSuggestion>
            {
                new DiagnosisSuggestion
                {
                    Diagnosis = "Major Depressive Disorder",
                    Confidence = 0.85,
                    Reasoning = "Symptoms align with DSM-5 criteria for MDD",
                    Severity = "Moderate"
                },
                new DiagnosisSuggestion
                {
                    Diagnosis = "Generalized Anxiety Disorder",
                    Confidence = 0.72,
                    Reasoning = "Chronic worry and anxiety symptoms present",
                    Severity = "Mild"
                },
                new DiagnosisSuggestion
                {
                    Diagnosis = "Bipolar Disorder",
                    Confidence = 0.45,
                    Reasoning = "Some mood fluctuation indicators",
                    Severity = "Moderate"
                }
            };
        }

        /// <summary>
        /// DEBUG: Test AI response directly to see what Hugging Face/TinyLlama returns
        /// </summary>
        [HttpGet("test-ai")]
        [AllowAnonymous] // Allow testing without auth
        public async Task<ActionResult<object>> TestAI([FromQuery] string diagnosis = "Depression", [FromQuery] int provider = 2)
        {
            try
            {
                var llmRequest = new LlmRequest
                {
                    Prompt = $"Generate clinical recommendations for diagnosis: {diagnosis}. Respond with JSON format only.",
                    Provider = provider == 1 ? AiProvider.HuggingFace : AiProvider.Ollama,
                    Model = provider == 1 ? "gpt2" : "tinyllama:latest",
                    Temperature = 0.3,
                    MaxTokens = 500
                };

                var response = await new LlmClient(HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration).GenerateTextAsync(llmRequest);

                return Ok(new
                {
                    provider = provider == 1 ? "HuggingFace" : "Ollama",
                    model = llmRequest.Model,
                    prompt = llmRequest.Prompt,
                    rawResponse = response.Text,
                    responseLength = response.Text?.Length ?? 0
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
