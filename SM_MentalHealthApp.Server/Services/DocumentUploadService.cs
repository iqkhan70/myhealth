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
        private readonly IServiceRequestService _serviceRequestService;

        public DocumentUploadService(
            JournalDbContext context,
            IAmazonS3 s3Client,
            IOptions<S3Config> s3Config,
            ILogger<DocumentUploadService> logger,
            IServiceRequestService serviceRequestService)
        {
            _context = context;
            _s3Client = s3Client;
            _s3Config = s3Config.Value;
            _logger = logger;
            _serviceRequestService = serviceRequestService;
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
                if (!FileValidationRules.IsValidFileType(request.MimeType, request.Type))
                {
                    return new DocumentUploadResponse
                    {
                        Success = false,
                        Message = $"File type {request.MimeType} is not allowed for {request.Type}."
                    };
                }

                if (!FileValidationRules.IsValidFileSize(request.MimeType, request.FileSizeBytes))
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
                    ServiceRequestId = request.ServiceRequestId, // Set ServiceRequestId
                    AddedByUserId = currentUserId,
                    Title = request.Title,
                    Description = request.Description ?? string.Empty,
                    FileName = request.FileName,
                    OriginalFileName = request.FileName,
                    MimeType = request.MimeType,
                    FileSizeBytes = request.FileSizeBytes,
                    ContentTypeModelId = await GetContentTypeIdAsync(request.Type),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Contents.Add(contentItem);
                await _context.SaveChangesAsync();

                // Generate S3 key
                var s3Key = $"documents/{request.PatientId}/{contentItem.ContentGuid}/{request.FileName}";

                // Generate pre-signed URL for upload
                var uploadUrl = await GeneratePresignedUploadUrlAsync(s3Key, request.MimeType);

                // Update content item with S3 info
                contentItem.S3Bucket = _s3Config.BucketName;
                contentItem.S3Key = s3Key;
                await _context.SaveChangesAsync();

                return new DocumentUploadResponse
                {
                    Success = true,
                    Message = "Upload initiated successfully.",
                    ContentId = contentItem.Id,
                    UploadUrl = uploadUrl,
                    S3Key = s3Key // Return the S3 key so client can use it for completion
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
                var currentUser = await _context.Users.FindAsync(currentUserId);
                if (currentUser == null)
                {
                    return new DocumentListResponse();
                }

                var query = _context.Contents
                    .Where(c => c.PatientId == request.PatientId && c.IsActive)
                    .Include(c => c.AddedByUser)
                    .AsQueryable();

                // ServiceRequest-based access control
                if (currentUser.RoleId == 1) // Patient
                {
                    // Patients see only their own documents
                    if (currentUser.Id != request.PatientId)
                    {
                        _logger.LogWarning("Patient {PatientId} tried to access documents for patient {RequestPatientId}", currentUser.Id, request.PatientId);
                        return new DocumentListResponse();
                    }
                    // If ServiceRequestId specified, filter by it
                    if (request.ServiceRequestId.HasValue)
                    {
                        // Check if this is the "General" ServiceRequest - if so, also include NULL documents
                        var generalSr = await _serviceRequestService.GetDefaultServiceRequestForClientAsync(request.PatientId);
                        if (generalSr != null && generalSr.Id == request.ServiceRequestId.Value)
                        {
                            // For General SR, show documents with this SR ID OR NULL (legacy documents)
                            query = query.Where(c => c.ServiceRequestId == request.ServiceRequestId.Value || c.ServiceRequestId == null);
                        }
                        else
                        {
                            // For non-General SRs, exact match only
                            query = query.Where(c => c.ServiceRequestId == request.ServiceRequestId.Value);
                        }
                    }
                }
                else if (currentUser.RoleId == 3) // Admin
                {
                    // Admin sees all documents, but can filter by ServiceRequestId if specified
                    if (request.ServiceRequestId.HasValue)
                    {
                        query = query.Where(c => c.ServiceRequestId == request.ServiceRequestId.Value);
                    }
                }
                else // Doctor, Coordinator, or Attorney
                {
                    // Get assigned ServiceRequest IDs for this SME
                    var serviceRequestIds = await _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId);

                    if (!serviceRequestIds.Any())
                    {
                        // No assigned service requests, return empty
                        return new DocumentListResponse();
                    }

                    // If specific SR requested, verify access
                    if (request.ServiceRequestId.HasValue)
                    {
                        if (!serviceRequestIds.Contains(request.ServiceRequestId.Value))
                        {
                            return new DocumentListResponse(); // Access denied
                        }
                        query = query.Where(c => c.ServiceRequestId == request.ServiceRequestId.Value);
                    }
                    else
                    {
                        // Filter to only documents in assigned ServiceRequests
                        query = query.Where(c => 
                            (c.ServiceRequestId.HasValue && serviceRequestIds.Contains(c.ServiceRequestId.Value)) ||
                            (!c.ServiceRequestId.HasValue && _context.ServiceRequests.Any(sr => 
                                sr.ClientId == request.PatientId && 
                                sr.Title == "General" && 
                                sr.IsActive && 
                                serviceRequestIds.Contains(sr.Id)
                            ))
                        );
                    }
                }

                // Apply filters
                if (request.Type.HasValue)
                {
                    var contentTypeId = await GetContentTypeIdAsync(request.Type.Value);
                    query = query.Where(c => c.ContentTypeModelId == contentTypeId);
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

                // Apply pagination and materialize the query first
                // This avoids EF Core translation issues with instance methods
                var contentItems = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                // Get content IDs for batch analysis queries
                var contentIds = contentItems.Select(c => c.Id).ToList();

                // Get analysis data in batch
                var analyses = await _context.ContentAnalyses
                    .Where(ca => contentIds.Contains(ca.ContentId))
                    .ToListAsync();

                // Map to DocumentInfo after materialization
                var documents = contentItems.Select(c =>
                {
                    var analysis = analyses.FirstOrDefault(a => a.ContentId == c.Id);
                    return new DocumentInfo
                    {
                        Id = c.Id,
                        ContentGuid = c.ContentGuid,
                        Title = c.Title,
                        Description = c.Description,
                        FileName = c.FileName,
                        OriginalFileName = c.OriginalFileName,
                        MimeType = c.MimeType,
                        FileSizeBytes = c.FileSizeBytes,
                        Type = GetContentTypeNameFromId(c.ContentTypeModelId),
                        CreatedAt = c.CreatedAt,
                        LastAccessedAt = c.LastAccessedAt,
                        IsActive = c.IsActive,
                        AddedByUserName = c.AddedByUser != null ? c.AddedByUser.FullName : "Unknown",
                        HasAnalysis = analysis != null,
                        AnalysisStatus = analysis?.ProcessingStatus ?? "Not Analyzed"
                    };
                }).ToList();

                // Generate download URLs
                foreach (var doc in documents)
                {
                    doc.DownloadUrl = await GetDownloadUrlAsync(doc.Id, currentUserId);
                    if (doc.Type == ContentTypeEnum.Image || doc.Type == ContentTypeEnum.Video)
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
                    MimeType = contentItem.MimeType,
                    FileSizeBytes = contentItem.FileSizeBytes,
                    Type = await GetContentTypeNameAsync(contentItem.ContentTypeModelId),
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
                if (documentInfo.Type == ContentTypeEnum.Image || documentInfo.Type == ContentTypeEnum.Video)
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

        private async Task<ContentTypeEnum> GetContentTypeNameAsync(int contentTypeId)
        {
            var contentType = await _context.ContentTypes
                .FirstOrDefaultAsync(ct => ct.Id == contentTypeId);

            if (contentType == null)
                return ContentTypeEnum.Document;

            return Enum.TryParse<ContentTypeEnum>(contentType.Name, out var result) ? result : ContentTypeEnum.Document;
        }

        private ContentTypeEnum GetContentTypeNameFromId(int contentTypeId)
        {
            // This is a simplified version for synchronous use
            // In a real implementation, you'd need to cache this or use a different approach
            return contentTypeId switch
            {
                1 => ContentTypeEnum.Document,
                2 => ContentTypeEnum.Image,
                3 => ContentTypeEnum.Video,
                4 => ContentTypeEnum.Audio,
                _ => ContentTypeEnum.Document
            };
        }
    }
}
