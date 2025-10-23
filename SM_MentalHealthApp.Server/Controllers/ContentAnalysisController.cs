using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using Microsoft.Extensions.Logging;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContentAnalysisController : ControllerBase
    {
        private readonly IContentAnalysisService _contentAnalysisService;
        private readonly IAuthService _authService;
        private readonly ILogger<ContentAnalysisController> _logger;

        public ContentAnalysisController(
            IContentAnalysisService contentAnalysisService,
            IAuthService authService,
            ILogger<ContentAnalysisController> logger)
        {
            _contentAnalysisService = contentAnalysisService;
            _authService = authService;
            _logger = logger;
        }

        [HttpGet("patient/{patientId}/alerts")]
        public async Task<ActionResult<List<Shared.ContentAlert>>> GetContentAlerts(int patientId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                {
                    return Unauthorized("Invalid or missing authentication token");
                }

                // Check if user has access to this patient's content
                if (user.RoleId == 1 && user.Id != patientId)
                {
                    return Forbid("Patients can only view their own content alerts");
                }

                if (user.RoleId == 2)
                {
                    // Doctor - check if patient is assigned to them
                    // This would need to be implemented in a service
                    // For now, allow access (you can add proper authorization later)
                }

                var alerts = await _contentAnalysisService.GenerateContentAlertsAsync(patientId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content alerts for patient {PatientId}", patientId);
                return StatusCode(500, "Error retrieving content alerts");
            }
        }

        [HttpGet("patient/{patientId}/analysis")]
        public async Task<ActionResult<List<Shared.ContentAnalysis>>> GetContentAnalysis(int patientId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                {
                    return Unauthorized("Invalid or missing authentication token");
                }

                // Check if user has access to this patient's content
                if (user.RoleId == 1 && user.Id != patientId)
                {
                    return Forbid("Patients can only view their own content analysis");
                }

                var analyses = await _contentAnalysisService.GetContentAnalysisForPatientAsync(patientId);
                return Ok(analyses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content analysis for patient {PatientId}", patientId);
                return StatusCode(500, "Error retrieving content analysis");
            }
        }

        [HttpPost("content/{contentId}/analyze")]
        public async Task<ActionResult<Shared.ContentAnalysis>> AnalyzeContent(int contentId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                {
                    return Unauthorized("Invalid or missing authentication token");
                }

                // This would need proper authorization to check if user can access this content
                // For now, allow access (you can add proper authorization later)

                // Get content item and analyze it
                // This would need to be implemented to get content by ID
                // For now, return a placeholder
                return Ok(new Shared.ContentAnalysis
                {
                    ContentId = contentId,
                    ContentTypeName = "Unknown",
                    ExtractedText = "Analysis not implemented yet",
                    AnalysisResults = new Dictionary<string, object>(),
                    Alerts = new List<string>(),
                    ProcessedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing content {ContentId}", contentId);
                return StatusCode(500, "Error analyzing content");
            }
        }

        private async Task<AuthUser?> GetCurrentUserAsync()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                return null;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            return await _authService.GetUserFromTokenAsync(token);
        }
    }
}
