using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IContentCleanupService
    {
        Task<Shared.ContentCleanupResult> CleanupDeletedContentAsync();
    }

    public class ContentCleanupService : IContentCleanupService
    {
        private readonly JournalDbContext _context;
        private readonly S3Service _s3Service;
        private readonly ILogger<ContentCleanupService> _logger;

        public ContentCleanupService(
            JournalDbContext context,
            S3Service s3Service,
            ILogger<ContentCleanupService> logger)
        {
            _context = context;
            _s3Service = s3Service;
            _logger = logger;
        }

        public async Task<Shared.ContentCleanupResult> CleanupDeletedContentAsync()
        {
            var result = new Shared.ContentCleanupResult();

            try
            {
                _logger.LogInformation("Starting cleanup of deleted content from S3...");

                // Find all content items that are marked as deleted (IsActive = false) and have an S3Key
                var deletedContent = await _context.Contents
                    .Where(c => !c.IsActive && !string.IsNullOrEmpty(c.S3Key))
                    .ToListAsync();

                result.TotalDeletedContentFound = deletedContent.Count;
                _logger.LogInformation("Found {Count} deleted content items to cleanup from S3", deletedContent.Count);

                foreach (var content in deletedContent)
                {
                    try
                    {
                        // Check if file still exists in S3
                        var fileExists = await _s3Service.FileExistsAsync(content.S3Key);

                        if (!fileExists)
                        {
                            result.AlreadyDeletedFromS3++;
                            _logger.LogInformation("Content ID {ContentId} (S3Key: {S3Key}) already deleted from S3, removing database record", content.Id, content.S3Key);
                            
                            // File already deleted from S3, remove the database record
                            await RemoveContentRecordAsync(content.Id);
                            continue;
                        }

                        // Delete from S3
                        var deleted = await _s3Service.DeleteFileAsync(content.S3Key);

                        if (deleted)
                        {
                            result.SuccessfullyDeletedFromS3++;
                            _logger.LogInformation("Successfully deleted content ID {ContentId} (S3Key: {S3Key}) from S3, removing database record", content.Id, content.S3Key);
                            
                            // Successfully deleted from S3, now remove the database record
                            await RemoveContentRecordAsync(content.Id);
                        }
                        else
                        {
                            result.FailedToDeleteFromS3++;
                            var error = $"Failed to delete content ID {content.Id} (S3Key: {content.S3Key}) from S3";
                            result.Errors.Add(error);
                            _logger.LogWarning(error);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedToDeleteFromS3++;
                        var error = $"Error deleting content ID {content.Id} (S3Key: {content.S3Key}) from S3: {ex.Message}";
                        result.Errors.Add(error);
                        _logger.LogError(ex, "Error deleting content ID {ContentId} from S3", content.Id);
                    }
                }

                _logger.LogInformation(
                    "Content cleanup completed. Total: {Total}, Success: {Success}, Failed: {Failed}, Already Deleted: {AlreadyDeleted}",
                    result.TotalDeletedContentFound,
                    result.SuccessfullyDeletedFromS3,
                    result.FailedToDeleteFromS3,
                    result.AlreadyDeletedFromS3);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during content cleanup");
                result.Errors.Add($"Fatal error: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Remove content record and related data from database
        /// </summary>
        private async Task RemoveContentRecordAsync(int contentId)
        {
            try
            {
                var content = await _context.Contents.FindAsync(contentId);
                if (content == null)
                {
                    _logger.LogWarning("Content ID {ContentId} not found in database", contentId);
                    return;
                }

                // Delete related ContentAnalysis records
                var analyses = await _context.ContentAnalyses
                    .Where(ca => ca.ContentId == contentId)
                    .ToListAsync();

                if (analyses.Any())
                {
                    _context.ContentAnalyses.RemoveRange(analyses);
                    _logger.LogInformation("Removed {Count} ContentAnalysis records for content ID {ContentId}", analyses.Count, contentId);
                }

                // Delete related ContentAlert records
                var alerts = await _context.ContentAlerts
                    .Where(ca => ca.ContentId == contentId)
                    .ToListAsync();

                if (alerts.Any())
                {
                    _context.ContentAlerts.RemoveRange(alerts);
                    _logger.LogInformation("Removed {Count} ContentAlert records for content ID {ContentId}", alerts.Count, contentId);
                }

                // Remove the content record itself
                _context.Contents.Remove(content);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully removed content ID {ContentId} and related records from database", contentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing content ID {ContentId} from database", contentId);
                throw; // Re-throw to be caught by the calling method
            }
        }
    }
}

