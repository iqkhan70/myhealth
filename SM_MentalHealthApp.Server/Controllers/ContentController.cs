using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using Amazon.S3;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContentController : ControllerBase
    {
        private readonly ContentService _contentService;
        private readonly ILogger<ContentController> _logger;
        private readonly IAmazonS3 _s3Client;

        public ContentController(ContentService contentService, ILogger<ContentController> logger, IAmazonS3 s3Client)
        {
            _contentService = contentService;
            _logger = logger;
            _s3Client = s3Client;
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

        [HttpGet("all")]
        public async Task<ActionResult<List<ContentItem>>> GetAllContents()
        {
            try
            {
                var contents = await _contentService.GetAllContentsAsync();
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
            try
            {
                if (request.File == null || request.File.Length == 0)
                    return BadRequest("No file provided");

                // Validate that the user can add content for this patient
                if (request.AddedByUserId.HasValue)
                {
                    // Get user role from database
                    var user = await _contentService.GetUserByIdAsync(request.AddedByUserId.Value);
                    if (user == null)
                        return BadRequest("User not found");

                    var canAccess = await _contentService.CanUserAddContentForPatientAsync(request.AddedByUserId.Value, user.RoleId, request.PatientId);
                    if (!canAccess)
                        return Forbid("You don't have permission to add content for this patient");
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
                    Description = request.Description,
                    FileName = $"{contentGuid}_{request.File.FileName}",
                    OriginalFileName = request.File.FileName,
                    ContentType = request.File.ContentType,
                    FileSizeBytes = request.File.Length,
                    Type = contentType
                };

                using var stream = request.File.OpenReadStream();
                var createdContent = await _contentService.CreateContentAsync(content, stream);

                return Ok(createdContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading content");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ContentItem>> UpdateContent(int id, [FromBody] ContentUpdateRequest request)
        {
            try
            {
                var content = await _contentService.GetContentByIdAsync(id);
                if (content == null)
                    return NotFound();

                // Validate that the user can modify this content
                if (request.AddedByUserId.HasValue)
                {
                    var user = await _contentService.GetUserByIdAsync(request.AddedByUserId.Value);
                    if (user == null)
                        return BadRequest("User not found");

                    var canAccess = await _contentService.CanUserAddContentForPatientAsync(request.AddedByUserId.Value, user.RoleId, content.PatientId);
                    if (!canAccess)
                        return Forbid("You don't have permission to modify content for this patient");
                }

                content.Title = request.Title;
                content.Description = request.Description;

                var updated = await _contentService.UpdateContentAsync(content);
                if (!updated)
                    return StatusCode(500, "Failed to update content");

                return Ok(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating content {ContentId}", id);
                return StatusCode(500, "Internal server error");
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

        private ContentType DetermineContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => ContentType.Image,
                ".mp4" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm" => ContentType.Video,
                ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" => ContentType.Audio,
                ".pdf" or ".doc" or ".docx" or ".txt" or ".rtf" or ".xls" or ".xlsx" or ".ppt" or ".pptx" => ContentType.Document,
                _ => ContentType.Other
            };
        }
    }

    public class ContentUploadRequest
    {
        public int PatientId { get; set; }
        public int? AddedByUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IFormFile File { get; set; } = null!;
    }

    public class ContentUpdateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? AddedByUserId { get; set; }
    }
}
