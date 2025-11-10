using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class ContentController : BaseController
    {
        private readonly ContentService _contentService;
        private readonly ILogger<ContentController> _logger;
        private readonly IAmazonS3 _s3Client;
        private readonly IContentAnalysisService _contentAnalysisService;
        private readonly JournalDbContext _context;

        public ContentController(ContentService contentService, ILogger<ContentController> logger, IAmazonS3 s3Client, IContentAnalysisService contentAnalysisService, JournalDbContext context)
        {
            _contentService = contentService;
            _logger = logger;
            _s3Client = s3Client;
            _contentAnalysisService = contentAnalysisService;
            _context = context;
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<List<ContentItem>>> GetContentsForPatient(int patientId)
        {
            try
            {
                var contents = await _contentService.GetContentsForPatientAsync(patientId);
                return Ok(contents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contents for patient {PatientId}", patientId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("test")]
        public async Task<ActionResult> TestDatabase()
        {
            try
            {
                var count = await _context.Contents.CountAsync();
                return Ok(new { message = "Database connection successful", count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database test failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("all")]
        public async Task<ActionResult> GetAllContents()
        {
            try
            {
                var contents = await _context.Contents
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.Id,
                        c.Title,
                        c.Description,
                        c.OriginalFileName,
                        c.FileSizeBytes,
                        c.CreatedAt,
                        c.ContentTypeModelId,
                        PatientId = c.PatientId,
                        AddedByUserId = c.AddedByUserId,
                        c.IsIgnoredByDoctor,
                        c.IgnoredByDoctorId,
                        c.IgnoredAt
                    })
                    .ToListAsync();
                return Ok(contents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all contents");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContentItem>> GetContent(int id)
        {
            try
            {
                var content = await _contentService.GetContentByIdAsync(id);
                if (content == null)
                    return NotFound();

                return Ok(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content {ContentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}/url")]
        public async Task<ActionResult<string>> GetContentUrl(int id)
        {
            try
            {
                var url = await _contentService.GetContentUrlAsync(id);
                return Ok(new { url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content URL for {ContentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("upload")]
        public async Task<ActionResult<ContentItem>> UploadContent([FromForm] ContentUploadRequest request)
        {
            _logger.LogInformation("=== UPLOAD ENDPOINT HIT ===");
            try
            {
                _logger.LogInformation("=== UPLOAD REQUEST START ===");
                _logger.LogInformation("Upload request received: PatientId={PatientId}, AddedByUserId={AddedByUserId}, Title='{Title}', Description='{Description}', FileName='{FileName}'",
                    request.PatientId, request.AddedByUserId, request.Title, request.Description, request.File?.FileName);

                // Check if request is null
                if (request == null)
                {
                    _logger.LogError("Request is null!");
                    return BadRequest("Request is null");
                }

                if (request.File == null || request.File.Length == 0)
                {
                    _logger.LogWarning("No file provided in upload request");
                    return BadRequest("No file provided");
                }

                // Validate that the user can add content for this patient
                if (request.AddedByUserId.HasValue)
                {
                    _logger.LogInformation("Validating user access: AddedByUserId={AddedByUserId}, PatientId={PatientId}", request.AddedByUserId.Value, request.PatientId);

                    // Get user role from database
                    var user = await _contentService.GetUserByIdAsync(request.AddedByUserId.Value);
                    if (user == null)
                    {
                        _logger.LogWarning("User not found: AddedByUserId={AddedByUserId}", request.AddedByUserId.Value);
                        return BadRequest("User not found");
                    }

                    _logger.LogInformation("User found: RoleId={RoleId}", user.RoleId);

                    var canAccess = await _contentService.CanUserAddContentForPatientAsync(request.AddedByUserId.Value, user.RoleId, request.PatientId);
                    _logger.LogInformation("Access check result: canAccess={canAccess}", canAccess);

                    if (!canAccess)
                    {
                        _logger.LogWarning("Access denied: AddedByUserId={AddedByUserId}, RoleId={RoleId}, PatientId={PatientId}", request.AddedByUserId.Value, user.RoleId, request.PatientId);
                        return Forbid("You don't have permission to add content for this patient");
                    }

                    _logger.LogInformation("Access granted for user {AddedByUserId} to add content for patient {PatientId}", request.AddedByUserId.Value, request.PatientId);
                }

                // Determine content type based on file extension
                var contentType = DetermineContentType(request.File.FileName);
                var contentGuid = Guid.NewGuid();

                var content = new ContentItem
                {
                    ContentGuid = contentGuid,
                    PatientId = request.PatientId,
                    AddedByUserId = request.AddedByUserId,
                    Title = request.Title,
                    Description = request.Description ?? string.Empty, // Provide empty string if null
                    FileName = $"{contentGuid}_{request.File.FileName}",
                    OriginalFileName = request.File.FileName,
                    MimeType = request.File.ContentType,
                    FileSizeBytes = request.File.Length,
                    ContentTypeModelId = await GetContentTypeIdAsync(contentType)
                };

                using var stream = request.File.OpenReadStream();
                var createdContent = await _contentService.CreateContentAsync(content, stream);

                _logger.LogInformation("Content created successfully with ID: {ContentId}", createdContent.Id);

                // Trigger content analysis synchronously to ensure it completes
                try
                {
                    _logger.LogInformation("Starting content analysis for content ID: {ContentId}", createdContent.Id);
                    var analysis = await _contentAnalysisService.AnalyzeContentAsync(createdContent);
                    _logger.LogInformation("Content analysis completed for content ID: {ContentId}. Alerts: {AlertCount}",
                        createdContent.Id, analysis.Alerts.Count);

                    if (analysis.Alerts.Any())
                    {
                        _logger.LogWarning("Content analysis generated alerts for content ID: {ContentId}: {Alerts}",
                            createdContent.Id, string.Join(", ", analysis.Alerts));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during content analysis for content ID: {ContentId}", createdContent.Id);
                }

                _logger.LogInformation("=== UPLOAD REQUEST SUCCESS ===");
                return Ok(createdContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== UPLOAD REQUEST ERROR ===");
                _logger.LogError("Error uploading content: {ErrorMessage}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateContent(int id, [FromBody] ContentUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Starting update for content {ContentId}", id);

                var content = await _context.Contents
                    .Where(c => c.Id == id && c.IsActive)
                    .Select(c => new ContentItem
                    {
                        Id = c.Id,
                        PatientId = c.PatientId,
                        AddedByUserId = c.AddedByUserId,
                        Title = c.Title,
                        Description = c.Description,
                        FileName = c.FileName,
                        OriginalFileName = c.OriginalFileName,
                        MimeType = c.MimeType,
                        FileSizeBytes = c.FileSizeBytes,
                        S3Bucket = c.S3Bucket,
                        S3Key = c.S3Key,
                        ContentTypeModelId = c.ContentTypeModelId,
                        CreatedAt = c.CreatedAt,
                        LastAccessedAt = c.LastAccessedAt,
                        IsActive = c.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (content == null)
                {
                    _logger.LogWarning("Content {ContentId} not found or not active", id);
                    return NotFound();
                }

                _logger.LogInformation("Found content {ContentId}: {Title}", id, content.Title);

                // Validate that the user can modify this content
                if (request.AddedByUserId.HasValue)
                {
                    _logger.LogInformation("Validating user {UserId} for content {ContentId}", request.AddedByUserId.Value, id);
                    var user = await _contentService.GetUserByIdAsync(request.AddedByUserId.Value);
                    if (user == null)
                    {
                        _logger.LogWarning("User {UserId} not found", request.AddedByUserId.Value);
                        return BadRequest("User not found");
                    }

                    var canAccess = await _contentService.CanUserAddContentForPatientAsync(request.AddedByUserId.Value, user.RoleId, content.PatientId);
                    if (!canAccess)
                    {
                        _logger.LogWarning("User {UserId} does not have permission to modify content {ContentId}", request.AddedByUserId.Value, id);
                        return Forbid("You don't have permission to modify content for this patient");
                    }
                }

                _logger.LogInformation("Updating content {ContentId} with title: {Title}", id, request.Title);
                content.Title = request.Title;
                content.Description = request.Description ?? string.Empty; // Provide empty string if null

                var updated = await _contentService.UpdateContentAsync(content);
                if (!updated)
                {
                    _logger.LogError("Failed to update content {ContentId}", id);
                    return StatusCode(500, "Failed to update content");
                }

                _logger.LogInformation("Successfully updated content {ContentId}", id);

                // Return a simplified response to avoid serialization issues
                var result = new
                {
                    content.Id,
                    content.Title,
                    content.Description,
                    content.OriginalFileName,
                    content.FileSizeBytes,
                    content.CreatedAt,
                    content.ContentTypeModelId,
                    PatientId = content.PatientId,
                    AddedByUserId = content.AddedByUserId
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating content {ContentId}: {Message}", id, ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteContent(int id)
        {
            try
            {
                var deleted = await _contentService.DeleteContentAsync(id);
                if (!deleted)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting content {ContentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("process-unanalyzed")]
        public async Task<ActionResult> ProcessAllUnanalyzedContent()
        {
            try
            {
                _logger.LogInformation("Processing all unanalyzed content...");
                await _contentAnalysisService.ProcessAllUnanalyzedContentAsync();
                _logger.LogInformation("Completed processing all unanalyzed content");
                return Ok(new { message = "All unanalyzed content has been processed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unanalyzed content");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Re-analyze a specific content item (Admin/Doctor only)
        /// Useful when content has critical values in ExtractedText but not in AnalysisResults
        /// </summary>
        [HttpPost("{id}/re-analyze")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult> ReAnalyzeContent(int id)
        {
            try
            {
                _logger.LogInformation("Re-analyzing content {ContentId}", id);
                var content = await _context.Contents.FindAsync(id);
                if (content == null)
                {
                    return NotFound("Content not found");
                }

                // Delete existing analysis to force re-analysis
                var existingAnalysis = await _context.ContentAnalyses
                    .FirstOrDefaultAsync(ca => ca.ContentId == id);
                if (existingAnalysis != null)
                {
                    _context.ContentAnalyses.Remove(existingAnalysis);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Deleted existing analysis for content {ContentId}", id);
                }

                // Re-analyze the content
                await _contentAnalysisService.AnalyzeContentAsync(content);
                _logger.LogInformation("Re-analysis completed for content {ContentId}", id);
                return Ok(new { message = "Content re-analyzed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-analyzing content {ContentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Toggle ignore status for a content item (doctors only)
        /// </summary>
        [HttpPost("{id}/toggle-ignore")]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult> ToggleIgnoreContent(int id)
        {
            try
            {
                var doctorId = GetCurrentUserId();
                if (!doctorId.HasValue)
                {
                    return Unauthorized("Doctor not authenticated");
                }

                var content = await _context.Contents.FindAsync(id);
                if (content == null)
                {
                    return NotFound("Content not found");
                }

                // Verify doctor has access to this patient's content
                var hasAccess = await _context.UserAssignments
                    .AnyAsync(ua => ua.AssignerId == doctorId.Value && ua.AssigneeId == content.PatientId && ua.IsActive);
                
                if (!hasAccess && content.PatientId != doctorId.Value)
                {
                    return Forbid("You can only ignore content for your assigned patients");
                }

                // Toggle ignore status
                if (content.IsIgnoredByDoctor)
                {
                    // Unignore
                    content.IsIgnoredByDoctor = false;
                    content.IgnoredByDoctorId = null;
                    content.IgnoredAt = null;
                    _logger.LogInformation("Content {ContentId} unignored by doctor {DoctorId}", id, doctorId);
                }
                else
                {
                    // Ignore
                    content.IsIgnoredByDoctor = true;
                    content.IgnoredByDoctorId = doctorId.Value;
                    content.IgnoredAt = DateTime.UtcNow;
                    _logger.LogInformation("Content {ContentId} ignored by doctor {DoctorId}", id, doctorId);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = content.IsIgnoredByDoctor ? "Content ignored" : "Content unignored" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling ignore status for content {ContentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("cleanup-orphaned-data")]
        public async Task<ActionResult> CleanupOrphanedData()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of orphaned data...");
                var cleanedCount = await _contentService.CleanupOrphanedDataAsync();
                _logger.LogInformation("Cleaned up {Count} orphaned records", cleanedCount);
                return Ok(new { message = $"Cleaned up {cleanedCount} orphaned records", cleanedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up orphaned data");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("test-s3")]
        public async Task<ActionResult> TestS3Connection()
        {
            try
            {
                // Test S3 connection by listing buckets
                var response = await _s3Client.ListBucketsAsync();
                return Ok(new
                {
                    success = true,
                    message = "S3 connection successful",
                    bucketCount = response.Buckets.Count,
                    buckets = response.Buckets.Select(b => b.BucketName).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 connection test failed");
                return StatusCode(500, new
                {
                    success = false,
                    message = "S3 connection failed",
                    error = ex.Message
                });
            }
        }

        private ContentTypeEnum DetermineContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => ContentTypeEnum.Image,
                ".mp4" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm" => ContentTypeEnum.Video,
                ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" => ContentTypeEnum.Audio,
                ".pdf" or ".doc" or ".docx" or ".txt" or ".rtf" or ".xls" or ".xlsx" or ".ppt" or ".pptx" => ContentTypeEnum.Document,
                _ => ContentTypeEnum.Other
            };
        }

        private async Task<int> GetContentTypeIdAsync(ContentTypeEnum type)
        {
            var contentTypeName = type.ToString();
            var contentType = await _context.ContentTypes
                .FirstOrDefaultAsync(ct => ct.Name == contentTypeName);

            if (contentType == null)
            {
                // Create the content type if it doesn't exist
                contentType = new ContentTypeModel
                {
                    Name = contentTypeName,
                    Description = $"Content type for {contentTypeName}",
                    Icon = GetIconForType(type),
                    IsActive = true,
                    SortOrder = (int)type,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ContentTypes.Add(contentType);
                await _context.SaveChangesAsync();
            }

            return contentType.Id;
        }

        private string GetIconForType(ContentTypeEnum type)
        {
            return type switch
            {
                ContentTypeEnum.Document => "📄",
                ContentTypeEnum.Image => "🖼️",
                ContentTypeEnum.Video => "🎥",
                ContentTypeEnum.Audio => "🎵",
                _ => "📁"
            };
        }
    }

    public class ContentUploadRequest
    {
        public int PatientId { get; set; }
        public int? AddedByUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IFormFile File { get; set; } = null!;
    }

    public class ContentUpdateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? AddedByUserId { get; set; }
    }

}
