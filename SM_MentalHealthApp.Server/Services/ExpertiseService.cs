using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IExpertiseService
    {
        Task<List<Expertise>> GetAllExpertisesAsync(bool activeOnly = true);
        Task<Expertise?> GetExpertiseByIdAsync(int id);
        Task<Expertise> CreateExpertiseAsync(string name, string? description = null);
        Task<Expertise?> UpdateExpertiseAsync(int id, string name, string? description = null, bool? isActive = null);
        Task<bool> DeleteExpertiseAsync(int id);
        Task<List<int>> GetExpertiseIdsForSmeAsync(int smeUserId);
        Task<bool> SetSmeExpertisesAsync(int smeUserId, List<int> expertiseIds);
        Task<List<int>> GetExpertiseIdsForServiceRequestAsync(int serviceRequestId);
        Task<bool> SetServiceRequestExpertisesAsync(int serviceRequestId, List<int> expertiseIds);
    }

    public class ExpertiseService : IExpertiseService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<ExpertiseService> _logger;

        public ExpertiseService(JournalDbContext context, ILogger<ExpertiseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Expertise>> GetAllExpertisesAsync(bool activeOnly = true)
        {
            var query = _context.Expertises.AsQueryable();
            if (activeOnly)
            {
                query = query.Where(e => e.IsActive);
            }
            return await query.OrderBy(e => e.Name).ToListAsync();
        }

        public async Task<Expertise?> GetExpertiseByIdAsync(int id)
        {
            return await _context.Expertises.FindAsync(id);
        }

        public async Task<Expertise> CreateExpertiseAsync(string name, string? description = null)
        {
            var expertise = new Expertise
            {
                Name = name,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Expertises.Add(expertise);
            await _context.SaveChangesAsync();
            return expertise;
        }

        public async Task<Expertise?> UpdateExpertiseAsync(int id, string name, string? description = null, bool? isActive = null)
        {
            var expertise = await _context.Expertises.FindAsync(id);
            if (expertise == null) return null;

            expertise.Name = name;
            if (description != null) expertise.Description = description;
            if (isActive.HasValue) expertise.IsActive = isActive.Value;
            expertise.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return expertise;
        }

        public async Task<bool> DeleteExpertiseAsync(int id)
        {
            var expertise = await _context.Expertises.FindAsync(id);
            if (expertise == null) return false;

            // Soft delete
            expertise.IsActive = false;
            expertise.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<int>> GetExpertiseIdsForSmeAsync(int smeUserId)
        {
            return await _context.SmeExpertises
                .Where(se => se.SmeUserId == smeUserId && se.IsActive)
                .Select(se => se.ExpertiseId)
                .ToListAsync();
        }

        public async Task<bool> SetSmeExpertisesAsync(int smeUserId, List<int> expertiseIds)
        {
            try
            {
                // Remove existing active expertise assignments
                var existing = await _context.SmeExpertises
                    .Where(se => se.SmeUserId == smeUserId && se.IsActive)
                    .ToListAsync();

                _context.SmeExpertises.RemoveRange(existing);

                // Add new expertise assignments
                foreach (var expertiseId in expertiseIds)
                {
                    // Check if expertise exists
                    var expertise = await _context.Expertises.FindAsync(expertiseId);
                    if (expertise == null || !expertise.IsActive) continue;

                    var smeExpertise = new SmeExpertise
                    {
                        SmeUserId = smeUserId,
                        ExpertiseId = expertiseId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.SmeExpertises.Add(smeExpertise);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting SME expertises for SME {SmeUserId}", smeUserId);
                return false;
            }
        }

        public async Task<List<int>> GetExpertiseIdsForServiceRequestAsync(int serviceRequestId)
        {
            return await _context.ServiceRequestExpertises
                .Where(sre => sre.ServiceRequestId == serviceRequestId)
                .Select(sre => sre.ExpertiseId)
                .ToListAsync();
        }

        public async Task<bool> SetServiceRequestExpertisesAsync(int serviceRequestId, List<int> expertiseIds)
        {
            try
            {
                // Remove existing expertise assignments
                var existing = await _context.ServiceRequestExpertises
                    .Where(sre => sre.ServiceRequestId == serviceRequestId)
                    .ToListAsync();

                _context.ServiceRequestExpertises.RemoveRange(existing);

                // Add new expertise assignments
                foreach (var expertiseId in expertiseIds)
                {
                    // Check if expertise exists
                    var expertise = await _context.Expertises.FindAsync(expertiseId);
                    if (expertise == null || !expertise.IsActive) continue;

                    var srExpertise = new ServiceRequestExpertise
                    {
                        ServiceRequestId = serviceRequestId,
                        ExpertiseId = expertiseId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ServiceRequestExpertises.Add(srExpertise);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting ServiceRequest expertises for SR {ServiceRequestId}", serviceRequestId);
                return false;
            }
        }
    }
}

