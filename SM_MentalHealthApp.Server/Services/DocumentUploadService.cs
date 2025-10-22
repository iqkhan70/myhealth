using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using System.Text.Json;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IDocumentUploadService
    {
        Task<DocumentUploadResponse> InitiateUploadAsync(DocumentUploadRequest request, int currentUserId);
        Task<DocumentUploadResponse> CompleteUploadAsync(int contentId, string s3Key);
        Task<DocumentListResponse> GetDocumentsAsync(DocumentListRequest request, int currentUserId);
        Task<DocumentInfo?> GetDocumentAsync(int contentId, int currentUserId);
        Task<DocumentDeleteResponse> DeleteDocumentAsync(int contentId, int currentUserId);
        Task<string> GetDownloadUrlAsync(int contentId, int currentUserId);
        Task<string> GetThumbnailUrlAsync(int contentId, int currentUserId);
        Task<bool> ValidateUserAccessAsync(int patientId, int currentUserId);
    }

    public class DocumentUploadService : IDocumentUploadService
    {
        private readonly JournalDbContext _context;
        private readonly IAmazonS3 _s3Client;
        private readonly S3Config _s3Config;
        private readonly ILogger<DocumentUploadService> _logger;

        public DocumentUploadService(
            JournalDbContext context,
            IAmazonS3 s3Client,
            IOptions<S3Config> s3Config,
            ILogger<DocumentUploadService> logger)
        {
            _context = context;
            _s3Client = s3Client;
            _s3Config = s3Config.Value;
            _logger = logger;
        }

        public async Task<DocumentUploadResponse> InitiateUploadAsync(DocumentUploadRequest request, int currentUserId)
        {
            try
            {
                // Validate user access
                if (!await ValidateUserAccessAsync(request.PatientId, currentUserId))
                {
                    return new DocumentUploadResponse
                    {
                        Success = false,
                        Message = "You don't have permission to upload documents for this patient."
                    };
                }

                // Validate file type and size
                if (!FileValidationRules.IsValidFileType(request.ContentType, request.Type))
                {
                    return new DocumentUploadResponse
                    {
                        Success = false,
                        Message = $"File type {request.ContentType} is not allowed for {request.Type}."
                    };
                }

                if (!FileValidationRules.IsValidFileSize(request.ContentType, request.FileSizeBytes))
                {
                    return new DocumentUploadResponse
                    {
                        Success = false,
                        Message = $"File size {request.FileSizeBytes} bytes exceeds the maximum allowed size."
                    };
                }

                // Create content item
                var contentItem = new ContentItem
                {
                    ContentGuid = Guid.NewGuid(),
                    PatientId = request.PatientId,
                    AddedByUserId = currentUserId,
                    Title = request.Title,
                    Description = request.Description ?? string.Empty,
                    FileName = request.FileName,
                    OriginalFileName = request.FileName,
                    ContentType = request.ContentType,
                    FileSizeBytes = request.FileSizeBytes,
                    Type = request.Type,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Contents.Add(contentItem);
                await _context.SaveChangesAsync();

                // Generate S3 key
                var s3Key = $"documents/{request.PatientId}/{contentItem.ContentGuid}/{request.FileName}";

                // Generate pre-signed URL for upload
                var uploadUrl = await GeneratePresignedUploadUrlAsync(s3Key, request.ContentType);

                // Update content item with S3 info
                contentItem.S3Bucket = _s3Config.BucketName;
                contentItem.S3Key = s3Key;
                await _context.SaveChangesAsync();

                return new DocumentUploadResponse
                {
                    Success = true,
                    Message = "Upload initiated successfully.",
                    ContentId = contentItem.Id,
                    UploadUrl = uploadUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating document upload for patient {PatientId}", request.PatientId);
                return new DocumentUploadResponse
                {
                    Success = false,
                    Message = "An error occurred while initiating the upload."
                };
            }
        }

        public async Task<DocumentUploadResponse> CompleteUploadAsync(int contentId, string s3Key)
        {
            try
            {
                var contentItem = await _context.Contents.FindAsync(contentId);
                if (contentItem == null)
                {
                    return new DocumentUploadResponse
                    {
                        Success = false,
                        Message = "Document not found."
                    };
                }

                // Verify the file exists in S3
                var exists = await DoesS3ObjectExistAsync(s3Key);
                if (!exists)
                {
                    return new DocumentUploadResponse
                    {
                        Success = false,
                        Message = "File upload verification failed."
                    };
                }

                return new DocumentUploadResponse
                {
                    Success = true,
                    Message = "Upload completed successfully.",
                    ContentId = contentId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing document upload for content {ContentId}", contentId);
                return new DocumentUploadResponse
                {
                    Success = false,
                    Message = "An error occurred while completing the upload."
                };
            }
        }

        public async Task<DocumentListResponse> GetDocumentsAsync(DocumentListRequest request, int currentUserId)
        {
            try
            {
                // Validate user access
                if (!await ValidateUserAccessAsync(request.PatientId, currentUserId))
                {
                    return new DocumentListResponse();
                }

                var query = _context.Contents
                    .Where(c => c.PatientId == request.PatientId && c.IsActive)
                    .Include(c => c.AddedByUser)
                    .AsQueryable();

                // Apply filters
                if (request.Type.HasValue)
                {
                    query = query.Where(c => c.Type == request.Type.Value);
                }

                if (!string.IsNullOrEmpty(request.Category))
                {
                    // Note: Category would need to be added to ContentItem model
                    // For now, we'll filter by title/description containing the category
                    query = query.Where(c => c.Title.Contains(request.Category) || c.Description.Contains(request.Category));
                }

                if (request.FromDate.HasValue)
                {
                    query = query.Where(c => c.CreatedAt >= request.FromDate.Value);
                }

                if (request.ToDate.HasValue)
                {
                    query = query.Where(c => c.CreatedAt <= request.ToDate.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var documents = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(c => new DocumentInfo
                    {
                        Id = c.Id,
                        ContentGuid = c.ContentGuid,
                        Title = c.Title,
                        Description = c.Description,
                        FileName = c.FileName,
                        OriginalFileName = c.OriginalFileName,
                        ContentType = c.ContentType,
                        FileSizeBytes = c.FileSizeBytes,
                        Type = c.Type,
                        CreatedAt = c.CreatedAt,
                        LastAccessedAt = c.LastAccessedAt,
                        IsActive = c.IsActive,
                        AddedByUserName = c.AddedByUser != null ? c.AddedByUser.FullName : "Unknown",
                        HasAnalysis = _context.ContentAnalyses.Any(ca => ca.ContentId == c.Id),
                        AnalysisStatus = _context.ContentAnalyses
                            .Where(ca => ca.ContentId == c.Id)
                            .Select(ca => ca.ProcessingStatus)
                            .FirstOrDefault() ?? "Not Analyzed"
                    })
                    .ToListAsync();

                // Generate download URLs
                foreach (var doc in documents)
                {
                    doc.DownloadUrl = await GetDownloadUrlAsync(doc.Id, currentUserId);
                    if (doc.Type == ContentType.Image || doc.Type == ContentType.Video)
                    {
                        doc.ThumbnailUrl = await GetThumbnailUrlAsync(doc.Id, currentUserId);
                    }
                }

                return new DocumentListResponse
                {
                    Documents = documents,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for patient {PatientId}", request.PatientId);
                return new DocumentListResponse();
            }
        }

        public async Task<DocumentInfo?> GetDocumentAsync(int contentId, int currentUserId)
        {
            try
            {
                var contentItem = await _context.Contents
                    .Include(c => c.AddedByUser)
                    .FirstOrDefaultAsync(c => c.Id == contentId && c.IsActive);

                if (contentItem == null)
                {
                    return null;
                }

                // Validate user access
                if (!await ValidateUserAccessAsync(contentItem.PatientId, currentUserId))
                {
                    return null;
                }

                var documentInfo = new DocumentInfo
                {
                    Id = contentItem.Id,
                    ContentGuid = contentItem.ContentGuid,
                    Title = contentItem.Title,
                    Description = contentItem.Description,
                    FileName = contentItem.FileName,
                    OriginalFileName = contentItem.OriginalFileName,
                    ContentType = contentItem.ContentType,
                    FileSizeBytes = contentItem.FileSizeBytes,
                    Type = contentItem.Type,
                    CreatedAt = contentItem.CreatedAt,
                    LastAccessedAt = contentItem.LastAccessedAt,
                    IsActive = contentItem.IsActive,
                    AddedByUserName = contentItem.AddedByUser?.FullName ?? "Unknown",
                    HasAnalysis = await _context.ContentAnalyses.AnyAsync(ca => ca.ContentId == contentId),
                    AnalysisStatus = await _context.ContentAnalyses
                        .Where(ca => ca.ContentId == contentId)
                        .Select(ca => ca.ProcessingStatus)
                        .FirstOrDefaultAsync() ?? "Not Analyzed"
                };

                // Generate URLs
                documentInfo.DownloadUrl = await GetDownloadUrlAsync(contentId, currentUserId);
                if (documentInfo.Type == ContentType.Image || documentInfo.Type == ContentType.Video)
                {
                    documentInfo.ThumbnailUrl = await GetThumbnailUrlAsync(contentId, currentUserId);
                }

                return documentInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {ContentId}", contentId);
                return null;
            }
        }

        public async Task<DocumentDeleteResponse> DeleteDocumentAsync(int contentId, int currentUserId)
        {
            try
            {
                var contentItem = await _context.Contents.FindAsync(contentId);
                if (contentItem == null)
                {
                    return new DocumentDeleteResponse
                    {
                        Success = false,
                        Message = "Document not found."
                    };
                }

                // Validate user access
                if (!await ValidateUserAccessAsync(contentItem.PatientId, currentUserId))
                {
                    return new DocumentDeleteResponse
                    {
                        Success = false,
                        Message = "You don't have permission to delete this document."
                    };
                }

                // Soft delete
                contentItem.IsActive = false;
                await _context.SaveChangesAsync();

                // TODO: Optionally delete from S3 as well
                // await DeleteFromS3Async(contentItem.S3Key);

                return new DocumentDeleteResponse
                {
                    Success = true,
                    Message = "Document deleted successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {ContentId}", contentId);
                return new DocumentDeleteResponse
                {
                    Success = false,
                    Message = "An error occurred while deleting the document."
                };
            }
        }

        public async Task<string> GetDownloadUrlAsync(int contentId, int currentUserId)
        {
            try
            {
                var contentItem = await _context.Contents.FindAsync(contentId);
                if (contentItem == null || !contentItem.IsActive)
                {
                    return string.Empty;
                }

                // Validate user access
                if (!await ValidateUserAccessAsync(contentItem.PatientId, currentUserId))
                {
                    return string.Empty;
                }

                // Update last accessed time
                contentItem.LastAccessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate pre-signed URL for download
                return await GeneratePresignedDownloadUrlAsync(contentItem.S3Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating download URL for content {ContentId}", contentId);
                return string.Empty;
            }
        }

        public async Task<string> GetThumbnailUrlAsync(int contentId, int currentUserId)
        {
            try
            {
                var contentItem = await _context.Contents.FindAsync(contentId);
                if (contentItem == null || !contentItem.IsActive)
                {
                    return string.Empty;
                }

                // Validate user access
                if (!await ValidateUserAccessAsync(contentItem.PatientId, currentUserId))
                {
                    return string.Empty;
                }

                // For now, return the same URL as download
                // In a real implementation, you'd generate thumbnails and store them separately
                return await GetDownloadUrlAsync(contentId, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating thumbnail URL for content {ContentId}", contentId);
                return string.Empty;
            }
        }

        public async Task<bool> ValidateUserAccessAsync(int patientId, int currentUserId)
        {
            try
            {
                var currentUser = await _context.Users.FindAsync(currentUserId);
                if (currentUser == null)
                {
                    return false;
                }

                // If user is the patient themselves
                if (currentUser.Id == patientId)
                {
                    return true;
                }

                // If user is a doctor assigned to this patient
                var isAssigned = await _context.UserAssignments
                    .AnyAsync(ua => ua.AssignerId == currentUserId && ua.AssigneeId == patientId && ua.IsActive);

                return isAssigned;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user access for patient {PatientId} and user {UserId}", patientId, currentUserId);
                return false;
            }
        }

        private async Task<string> GeneratePresignedUploadUrlAsync(string s3Key, string contentType)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _s3Config.BucketName,
                Key = s3Key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddHours(1),
                ContentType = contentType
            };

            return await _s3Client.GetPreSignedURLAsync(request);
        }

        private async Task<string> GeneratePresignedDownloadUrlAsync(string s3Key)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _s3Config.BucketName,
                Key = s3Key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(1)
            };

            return await _s3Client.GetPreSignedURLAsync(request);
        }

        private async Task<bool> DoesS3ObjectExistAsync(string s3Key)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = _s3Config.BucketName,
                    Key = s3Key
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}
