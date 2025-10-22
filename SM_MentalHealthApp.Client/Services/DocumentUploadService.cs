using System.Net.Http.Json;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services
{
    public interface IDocumentUploadService
    {
        Task<DocumentUploadResponse> InitiateUploadAsync(DocumentUploadRequest request);
        Task<DocumentUploadResponse> CompleteUploadAsync(int contentId, string s3Key);
        Task<DocumentListResponse> GetDocumentsAsync(DocumentListRequest request);
        Task<DocumentInfo?> GetDocumentAsync(int contentId);
        Task<DocumentDeleteResponse> DeleteDocumentAsync(int contentId);
        Task<string> GetDownloadUrlAsync(int contentId);
        Task<string> GetThumbnailUrlAsync(int contentId);
        Task<string[]> GetCategoriesAsync();
        Task<FileValidationRulesResponse> GetValidationRulesAsync();
    }

    public class DocumentUploadService : IDocumentUploadService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DocumentUploadService> _logger;

        public DocumentUploadService(HttpClient httpClient, ILogger<DocumentUploadService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<DocumentUploadResponse> InitiateUploadAsync(DocumentUploadRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/documentupload/initiate", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DocumentUploadResponse>() ?? new DocumentUploadResponse { Success = false, Message = "Invalid response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating document upload");
                return new DocumentUploadResponse { Success = false, Message = "Failed to initiate upload" };
            }
        }

        public async Task<DocumentUploadResponse> CompleteUploadAsync(int contentId, string s3Key)
        {
            try
            {
                var request = new CompleteUploadRequest { S3Key = s3Key };
                var response = await _httpClient.PostAsJsonAsync($"api/documentupload/complete/{contentId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DocumentUploadResponse>() ?? new DocumentUploadResponse { Success = false, Message = "Invalid response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing document upload");
                return new DocumentUploadResponse { Success = false, Message = "Failed to complete upload" };
            }
        }

        public async Task<DocumentListResponse> GetDocumentsAsync(DocumentListRequest request)
        {
            try
            {
                var queryParams = new List<string>();
                queryParams.Add($"patientId={request.PatientId}");
                queryParams.Add($"page={request.Page}");
                queryParams.Add($"pageSize={request.PageSize}");

                if (request.Type.HasValue)
                    queryParams.Add($"type={request.Type.Value}");
                if (!string.IsNullOrEmpty(request.Category))
                    queryParams.Add($"category={Uri.EscapeDataString(request.Category)}");
                if (request.FromDate.HasValue)
                    queryParams.Add($"fromDate={request.FromDate.Value:yyyy-MM-dd}");
                if (request.ToDate.HasValue)
                    queryParams.Add($"toDate={request.ToDate.Value:yyyy-MM-dd}");

                var response = await _httpClient.GetAsync($"api/documentupload/list?{string.Join("&", queryParams)}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DocumentListResponse>() ?? new DocumentListResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents");
                return new DocumentListResponse();
            }
        }

        public async Task<DocumentInfo?> GetDocumentAsync(int contentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/documentupload/{contentId}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DocumentInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {ContentId}", contentId);
                return null;
            }
        }

        public async Task<DocumentDeleteResponse> DeleteDocumentAsync(int contentId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/documentupload/{contentId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DocumentDeleteResponse>() ?? new DocumentDeleteResponse { Success = false, Message = "Invalid response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {ContentId}", contentId);
                return new DocumentDeleteResponse { Success = false, Message = "Failed to delete document" };
            }
        }

        public async Task<string> GetDownloadUrlAsync(int contentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/documentupload/{contentId}/download");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return string.Empty;

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<DownloadUrlResponse>();
                return result?.DownloadUrl ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting download URL for content {ContentId}", contentId);
                return string.Empty;
            }
        }

        public async Task<string> GetThumbnailUrlAsync(int contentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/documentupload/{contentId}/thumbnail");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return string.Empty;

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<ThumbnailUrlResponse>();
                return result?.ThumbnailUrl ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting thumbnail URL for content {ContentId}", contentId);
                return string.Empty;
            }
        }

        public async Task<string[]> GetCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/documentupload/categories");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<DocumentCategoriesResponse>();
                return result?.Categories ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return Array.Empty<string>();
            }
        }

        public async Task<FileValidationRulesResponse> GetValidationRulesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/documentupload/validation-rules");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<FileValidationRulesResponse>() ?? new FileValidationRulesResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting validation rules");
                return new FileValidationRulesResponse();
            }
        }
    }

    // Additional models for client
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
