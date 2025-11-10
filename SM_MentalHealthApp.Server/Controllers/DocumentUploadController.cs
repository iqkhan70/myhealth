using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using System.Security.Claims;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentUploadController : BaseController
    {
        private readonly IDocumentUploadService _documentUploadService;
        private readonly ILogger<DocumentUploadController> _logger;

        public DocumentUploadController(
            IDocumentUploadService documentUploadService,
            ILogger<DocumentUploadController> logger)
        {
            _documentUploadService = documentUploadService;
            _logger = logger;
        }

        /// <summary>
        /// Initiate a document upload by creating a pre-signed URL
        /// </summary>
        [HttpPost("initiate")]
        public async Task<ActionResult<DocumentUploadResponse>> InitiateUpload([FromBody] DocumentUploadRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                var response = await _documentUploadService.InitiateUploadAsync(request, currentUserId.Value);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating document upload");
                return StatusCode(500, new DocumentUploadResponse
                {
                    Success = false,
                    Message = "An internal error occurred"
                });
            }
        }

        /// <summary>
        /// Complete a document upload after file has been uploaded to S3
        /// </summary>
        [HttpPost("complete/{contentId}")]
        public async Task<ActionResult<DocumentUploadResponse>> CompleteUpload(int contentId, [FromBody] CompleteUploadRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                var response = await _documentUploadService.CompleteUploadAsync(contentId, request.S3Key);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing document upload for content {ContentId}", contentId);
                return StatusCode(500, new DocumentUploadResponse
                {
                    Success = false,
                    Message = "An internal error occurred"
                });
            }
        }

        /// <summary>
        /// Get list of documents for a patient
        /// </summary>
        [HttpGet("list")]
        public async Task<ActionResult<DocumentListResponse>> GetDocuments([FromQuery] DocumentListRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                var response = await _documentUploadService.GetDocumentsAsync(request, currentUserId.Value);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for patient {PatientId}", request.PatientId);
                return StatusCode(500, new DocumentListResponse());
            }
        }

        /// <summary>
        /// Get specific document information
        /// </summary>
        [HttpGet("{contentId}")]
        public async Task<ActionResult<DocumentInfo>> GetDocument(int contentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                var document = await _documentUploadService.GetDocumentAsync(contentId, currentUserId.Value);

                if (document == null)
                {
                    return NotFound("Document not found or access denied");
                }

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {ContentId}", contentId);
                return StatusCode(500, "An internal error occurred");
            }
        }

        /// <summary>
        /// Get download URL for a document
        /// </summary>
        [HttpGet("{contentId}/download")]
        public async Task<ActionResult<DownloadUrlResponse>> GetDownloadUrl(int contentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                var downloadUrl = await _documentUploadService.GetDownloadUrlAsync(contentId, currentUserId.Value);

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    return NotFound("Document not found or access denied");
                }

                return Ok(new DownloadUrlResponse { DownloadUrl = downloadUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting download URL for content {ContentId}", contentId);
                return StatusCode(500, "An internal error occurred");
            }
        }

        /// <summary>
        /// Get thumbnail URL for a document (images/videos)
        /// </summary>
        [HttpGet("{contentId}/thumbnail")]
        public async Task<ActionResult<ThumbnailUrlResponse>> GetThumbnailUrl(int contentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                var thumbnailUrl = await _documentUploadService.GetThumbnailUrlAsync(contentId, currentUserId.Value);

                if (string.IsNullOrEmpty(thumbnailUrl))
                {
                    return NotFound("Thumbnail not found or access denied");
                }

                return Ok(new ThumbnailUrlResponse { ThumbnailUrl = thumbnailUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting thumbnail URL for content {ContentId}", contentId);
                return StatusCode(500, "An internal error occurred");
            }
        }

        /// <summary>
        /// Delete a document
        /// </summary>
        [HttpDelete("{contentId}")]
        public async Task<ActionResult<DocumentDeleteResponse>> DeleteDocument(int contentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                var response = await _documentUploadService.DeleteDocumentAsync(contentId, currentUserId.Value);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {ContentId}", contentId);
                return StatusCode(500, new DocumentDeleteResponse
                {
                    Success = false,
                    Message = "An internal error occurred"
                });
            }
        }

        /// <summary>
        /// Get available document categories
        /// </summary>
        [HttpGet("categories")]
        public ActionResult<DocumentCategoriesResponse> GetCategories()
        {
            return Ok(new DocumentCategoriesResponse
            {
                Categories = DocumentCategories.All
            });
        }

        /// <summary>
        /// Get file validation rules
        /// </summary>
        [HttpGet("validation-rules")]
        public ActionResult<FileValidationRulesResponse> GetValidationRules()
        {
            return Ok(new FileValidationRulesResponse
            {
                MaxFileSizes = FileValidationRules.MaxFileSizes,
                AllowedImageTypes = FileValidationRules.AllowedImageTypes,
                AllowedVideoTypes = FileValidationRules.AllowedVideoTypes,
                AllowedAudioTypes = FileValidationRules.AllowedAudioTypes,
                AllowedDocumentTypes = FileValidationRules.AllowedDocumentTypes
            });
        }

    }

    // Additional request/response models
    public class CompleteUploadRequest
    {
        public string S3Key { get; set; } = string.Empty;
    }

    public class DownloadUrlResponse
    {
        public string DownloadUrl { get; set; } = string.Empty;
    }

    public class ThumbnailUrlResponse
    {
        public string ThumbnailUrl { get; set; } = string.Empty;
    }

    public class DocumentCategoriesResponse
    {
        public string[] Categories { get; set; } = Array.Empty<string>();
    }

    public class FileValidationRulesResponse
    {
        public Dictionary<string, long> MaxFileSizes { get; set; } = new();
        public string[] AllowedImageTypes { get; set; } = Array.Empty<string>();
        public string[] AllowedVideoTypes { get; set; } = Array.Empty<string>();
        public string[] AllowedAudioTypes { get; set; } = Array.Empty<string>();
        public string[] AllowedDocumentTypes { get; set; } = Array.Empty<string>();
    }
}
