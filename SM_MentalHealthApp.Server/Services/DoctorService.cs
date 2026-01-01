using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Helpers;
using SM_MentalHealthApp.Server.Services;
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
        private readonly IPiiEncryptionService _encryptionService;

        public DoctorService(JournalDbContext context, IPiiEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<List<User>> GetMyPatientsAsync(int doctorId)
        {
            // Get clients from ServiceRequests assigned to this SME
            var serviceRequestIds = await _context.ServiceRequestAssignments
                .Where(a => a.SmeUserId == doctorId && a.IsActive)
                .Select(a => a.ServiceRequestId)
                .ToListAsync();
            
            var clientIds = await _context.ServiceRequests
                .Where(sr => serviceRequestIds.Contains(sr.Id) && sr.IsActive)
                .Select(sr => sr.ClientId)
                .Distinct()
                .ToListAsync();
            
            var patients = await _context.Users
                .Include(u => u.Role)
                .Where(u => clientIds.Contains(u.Id) && u.IsActive && u.RoleId == 1)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
            
            // Decrypt DateOfBirth for all patients
            UserEncryptionHelper.DecryptUserData(patients, _encryptionService);
            
            return patients;
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

            // Check if target doctor or attorney exists and is active
            var targetDoctor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == toDoctorId && (u.RoleId == 2 || u.RoleId == 5 || u.RoleId == 6) && u.IsActive);

            if (targetDoctor == null)
            {
                return false; // Invalid target doctor or attorney
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
