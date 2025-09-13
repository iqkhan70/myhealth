using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public class ContentService
    {
        private readonly JournalDbContext _context;
        private readonly S3Service _s3Service;
        private readonly S3Config _s3Config;

        public ContentService(JournalDbContext context, S3Service s3Service, IOptions<S3Config> s3Config)
        {
            _context = context;
            _s3Service = s3Service;
            _s3Config = s3Config.Value;
        }

        public async Task<List<ContentItem>> GetContentsForPatientAsync(int patientId)
        {
            return await _context.Contents
                .Include(c => c.Patient)
                .Include(c => c.AddedByUser)
                .Where(c => c.PatientId == patientId && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ContentItem>> GetAllContentsAsync()
        {
            return await _context.Contents
                .Include(c => c.Patient)
                .Include(c => c.AddedByUser)
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<ContentItem?> GetContentByIdAsync(int id)
        {
            return await _context.Contents
                .Include(c => c.Patient)
                .Include(c => c.AddedByUser)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        }

        public async Task<ContentItem?> GetContentByGuidAsync(Guid contentGuid)
        {
            return await _context.Contents
                .Include(c => c.Patient)
                .Include(c => c.AddedByUser)
                .FirstOrDefaultAsync(c => c.ContentGuid == contentGuid && c.IsActive);
        }

        public async Task<ContentItem> CreateContentAsync(ContentItem content, Stream fileStream)
        {
            try
            {
                // Upload file to S3
                var s3Key = await _s3Service.UploadFileAsync(
                    fileStream,
                    content.FileName,
                    content.ContentType,
                    content.ContentGuid
                );

                // Update content with S3 information
                content.S3Key = s3Key;
                content.S3Bucket = _s3Config.BucketName;

                // Generate presigned URL
                content.S3Url = await _s3Service.GetPresignedUrlAsync(s3Key);

                // Save to database
                _context.Contents.Add(content);
                await _context.SaveChangesAsync();

                return content;
            }
            catch (Exception ex)
            {
                // If database save fails, try to clean up S3 file
                if (!string.IsNullOrEmpty(content.S3Key))
                {
                    try
                    {
                        await _s3Service.DeleteFileAsync(content.S3Key);
                    }
                    catch
                    {
                        // Log cleanup failure but don't throw
                    }
                }
                throw new Exception($"Failed to create content: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateContentAsync(ContentItem content)
        {
            try
            {
                _context.Contents.Update(content);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update content: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteContentAsync(int id)
        {
            try
            {
                var content = await _context.Contents.FindAsync(id);
                if (content == null)
                    return false;

                // Delete from S3
                if (!string.IsNullOrEmpty(content.S3Key))
                {
                    await _s3Service.DeleteFileAsync(content.S3Key);
                }

                // Soft delete from database
                content.IsActive = false;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete content: {ex.Message}", ex);
            }
        }

        public async Task<string> GetContentUrlAsync(int id)
        {
            var content = await _context.Contents.FindAsync(id);
            if (content == null || string.IsNullOrEmpty(content.S3Key))
                throw new Exception("Content not found");

            // Update last accessed time
            content.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate fresh presigned URL
            return await _s3Service.GetPresignedUrlAsync(content.S3Key);
        }

        public async Task<bool> CanUserAccessContentAsync(int contentId, int userId, int userRoleId)
        {
            var content = await _context.Contents
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.Id == contentId && c.IsActive);

            if (content == null)
                return false;

            // Admin can access all content
            if (userRoleId == 3)
                return true;

            // Doctor can access content for their assigned patients
            if (userRoleId == 2)
            {
                var assignment = await _context.UserAssignments
                    .FirstOrDefaultAsync(a => a.AssignerId == userId && a.AssigneeId == content.PatientId);
                return assignment != null;
            }

            // Patient can only access their own content
            if (userRoleId == 1)
                return content.PatientId == userId;

            return false;
        }

        public async Task<bool> CanUserDeleteContentAsync(int userId, int userRoleId)
        {
            // Only admins can delete content
            return userRoleId == 3;
        }

        public async Task<bool> CanUserAddContentForPatientAsync(int userId, int userRoleId, int patientId)
        {
            // Admin can add content for any patient
            if (userRoleId == 3)
                return true;

            // Patient can only add content for themselves
            if (userRoleId == 1)
                return userId == patientId;

            // Doctor can only add content for assigned patients
            if (userRoleId == 2)
            {
                var assignment = await _context.UserAssignments
                    .FirstOrDefaultAsync(a => a.AssignerId == userId && a.AssigneeId == patientId);
                return assignment != null;
            }

            return false;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }
    }
}
