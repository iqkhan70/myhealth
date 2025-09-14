using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IDoctorService
    {
        Task<List<User>> GetMyPatientsAsync(int doctorId);
        Task<List<User>> GetAllDoctorsAsync();
        Task<bool> AssignMyPatientToDoctorAsync(int patientId, int fromDoctorId, int toDoctorId);
        Task<bool> UnassignMyPatientFromMeAsync(int patientId, int doctorId);
        Task<bool> IsPatientAssignedToMeAsync(int patientId, int doctorId);
    }

    public class DoctorService : IDoctorService
    {
        private readonly JournalDbContext _context;

        public DoctorService(JournalDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetMyPatientsAsync(int doctorId)
        {
            return await _context.UserAssignments
                .Where(ua => ua.AssignerId == doctorId)
                .Include(ua => ua.Assignee)
                .ThenInclude(a => a.Role)
                .Select(ua => ua.Assignee)
                .Where(p => p.IsActive)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
        }

        public async Task<List<User>> GetAllDoctorsAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RoleId == 2 && u.IsActive) // Role 2 = Doctor
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<bool> AssignMyPatientToDoctorAsync(int patientId, int fromDoctorId, int toDoctorId)
        {
            // First verify that the patient is actually assigned to the requesting doctor
            var currentAssignment = await _context.UserAssignments
                .FirstOrDefaultAsync(ua => ua.AssigneeId == patientId && ua.AssignerId == fromDoctorId);

            if (currentAssignment == null)
            {
                return false; // Patient is not assigned to the requesting doctor
            }

            // Check if assignment to the target doctor already exists
            var existingAssignment = await _context.UserAssignments
                .FirstOrDefaultAsync(ua => ua.AssigneeId == patientId && ua.AssignerId == toDoctorId);

            if (existingAssignment != null)
            {
                return false; // Already assigned to target doctor
            }

            // Check if target doctor exists and is active
            var targetDoctor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == toDoctorId && u.RoleId == 2 && u.IsActive);

            if (targetDoctor == null)
            {
                return false; // Invalid target doctor
            }

            // Create new assignment to target doctor
            var newAssignment = new UserAssignment
            {
                AssignerId = toDoctorId,
                AssigneeId = patientId,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserAssignments.Add(newAssignment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnassignMyPatientFromMeAsync(int patientId, int doctorId)
        {
            // Find the assignment where the requesting doctor is the assigner
            var assignment = await _context.UserAssignments
                .FirstOrDefaultAsync(ua => ua.AssigneeId == patientId && ua.AssignerId == doctorId);

            if (assignment == null)
            {
                return false; // Assignment doesn't exist or patient is not assigned to this doctor
            }

            _context.UserAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsPatientAssignedToMeAsync(int patientId, int doctorId)
        {
            var assignment = await _context.UserAssignments
                .FirstOrDefaultAsync(ua => ua.AssigneeId == patientId && ua.AssignerId == doctorId);

            return assignment != null;
        }
    }
}
