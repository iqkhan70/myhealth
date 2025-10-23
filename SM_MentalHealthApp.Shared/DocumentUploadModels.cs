using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    // Document upload request from mobile/web client
    public class DocumentUploadRequest
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string MimeType { get; set; } = string.Empty;

        [Required]
        public long FileSizeBytes { get; set; }

        [Required]
        public ContentTypeEnum Type { get; set; }

        public string? Category { get; set; } // "Test Results", "Prescription", "X-Ray", "Lab Report", etc.

        public Dictionary<string, string>? Metadata { get; set; } // Additional metadata
    }

    // Document upload response
    public class DocumentUploadResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ContentId { get; set; }
        public string? UploadUrl { get; set; } // For direct S3 upload
        public Dictionary<string, string>? UploadFields { get; set; } // S3 form fields
    }

    // Document list request
    public class DocumentListRequest
    {
        public int PatientId { get; set; }
        public ContentTypeEnum? Type { get; set; }
        public string? Category { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // Document list response
    public class DocumentListResponse
    {
        public List<DocumentInfo> Documents { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    // Document information for display
    public class DocumentInfo
    {
        public int Id { get; set; }
        public Guid ContentGuid { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public ContentTypeEnum Type { get; set; }
        public string? Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public bool IsActive { get; set; }
        public string? AddedByUserName { get; set; }
        public string? DownloadUrl { get; set; } // Pre-signed URL for download
        public string? ThumbnailUrl { get; set; } // For images/videos
        public bool HasAnalysis { get; set; }
        public string? AnalysisStatus { get; set; }
    }

    // Document delete request
    public class DocumentDeleteRequest
    {
        [Required]
        public int ContentId { get; set; }

        [Required]
        public int PatientId { get; set; }
    }

    // Document delete response
    public class DocumentDeleteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Document categories for medical documents
    public static class DocumentCategories
    {
        public const string TestResults = "Test Results";
        public const string Prescription = "Prescription";
        public const string XRay = "X-Ray";
        public const string LabReport = "Lab Report";
        public const string MedicalRecord = "Medical Record";
        public const string Insurance = "Insurance";
        public const string Referral = "Referral";
        public const string DischargeSummary = "Discharge Summary";
        public const string Consultation = "Consultation";
        public const string Other = "Other";

        public static readonly string[] All = {
            TestResults, Prescription, XRay, LabReport, MedicalRecord,
            Insurance, Referral, DischargeSummary, Consultation, Other
        };
    }

    // File validation rules
    public static class FileValidationRules
    {
        public static readonly Dictionary<string, long> MaxFileSizes = new()
        {
            { "image/jpeg", 10 * 1024 * 1024 }, // 10MB
            { "image/png", 10 * 1024 * 1024 },  // 10MB
            { "image/gif", 10 * 1024 * 1024 },  // 10MB
            { "video/mp4", 100 * 1024 * 1024 }, // 100MB
            { "video/avi", 100 * 1024 * 1024 }, // 100MB
            { "video/mov", 100 * 1024 * 1024 }, // 100MB
            { "audio/mp3", 50 * 1024 * 1024 },  // 50MB
            { "audio/wav", 50 * 1024 * 1024 },  // 50MB
            { "application/pdf", 25 * 1024 * 1024 }, // 25MB
            { "application/msword", 25 * 1024 * 1024 }, // 25MB
            { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 25 * 1024 * 1024 }, // 25MB
        };

        public static readonly string[] AllowedImageTypes = {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
        };

        public static readonly string[] AllowedVideoTypes = {
            "video/mp4", "video/avi", "video/mov", "video/quicktime", "video/x-msvideo"
        };

        public static readonly string[] AllowedAudioTypes = {
            "audio/mp3", "audio/wav", "audio/mpeg", "audio/x-wav"
        };

        public static readonly string[] AllowedDocumentTypes = {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain"
        };

        public static bool IsValidFileType(string contentType, ContentTypeEnum type)
        {
            return type switch
            {
                ContentTypeEnum.Image => AllowedImageTypes.Contains(contentType),
                ContentTypeEnum.Video => AllowedVideoTypes.Contains(contentType),
                ContentTypeEnum.Audio => AllowedAudioTypes.Contains(contentType),
                ContentTypeEnum.Document => AllowedDocumentTypes.Contains(contentType),
                _ => false
            };
        }

        public static bool IsValidFileSize(string contentType, long fileSize)
        {
            if (MaxFileSizes.TryGetValue(contentType, out long maxSize))
            {
                return fileSize <= maxSize;
            }
            return fileSize <= 10 * 1024 * 1024; // Default 10MB limit
        }
    }
}
