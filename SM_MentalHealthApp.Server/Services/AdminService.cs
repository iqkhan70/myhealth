using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Helpers;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IAdminService
    {
        Task<List<User>> GetAllDoctorsAsync();
        Task<List<User>> GetAllPatientsAsync();
        Task<List<User>> GetAllCoordinatorsAsync();
        Task<List<User>> GetAllAttorneysAsync();
        Task<List<UserAssignment>> GetUserAssignmentsAsync();
        Task<bool> AssignPatientToDoctorAsync(int patientId, int doctorId);
        Task<bool> UnassignPatientFromDoctorAsync(int patientId, int doctorId);
        Task<List<User>> GetPatientsForDoctorAsync(int doctorId);
        Task<List<User>> GetDoctorsForPatientAsync(int patientId);
    }

    public class AdminService : IAdminService
    {
        private readonly JournalDbContext _context;
        private readonly IPiiEncryptionService _encryptionService;

        public AdminService(JournalDbContext context, IPiiEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<List<User>> GetAllDoctorsAsync()
        {
            var doctors = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RoleId == 2 && u.IsActive) // Role 2 = Doctor
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
            
            UserEncryptionHelper.DecryptUserData(doctors, _encryptionService);
            return doctors;
        }

        public async Task<List<User>> GetAllPatientsAsync()
        {
            var patients = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RoleId == 1 && u.IsActive) // Role 1 = Patient
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
            
            UserEncryptionHelper.DecryptUserData(patients, _encryptionService);
            return patients;
        }

        public async Task<List<User>> GetAllCoordinatorsAsync()
        {
            var coordinators = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RoleId == 4 && u.IsActive) // Role 4 = Coordinator
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
            
            UserEncryptionHelper.DecryptUserData(coordinators, _encryptionService);
            return coordinators;
        }

        public async Task<List<User>> GetAllAttorneysAsync()
        {
            var attorneys = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RoleId == 5 && u.IsActive) // Role 5 = Attorney
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
            
            UserEncryptionHelper.DecryptUserData(attorneys, _encryptionService);
            return attorneys;
        }

        public async Task<List<UserAssignment>> GetUserAssignmentsAsync()
        {
            return await _context.UserAssignments
                .Include(ua => ua.Assigner)
                .Include(ua => ua.Assignee)
                .ToListAsync();
        }

        public async Task<bool> AssignPatientToDoctorAsync(int patientId, int doctorId)
        {
            // Check if assignment already exists
            var existingAssignment = await _context.UserAssignments
                .FirstOrDefaultAsync(ua => ua.AssigneeId == patientId && ua.AssignerId == doctorId);

            if (existingAssignment != null)
            {
                return false; // Already assigned
            }

            // Check if patient exists and is active
            var patient = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == patientId && u.RoleId == 1 && u.IsActive);

            if (patient == null)
            {
                return false; // Invalid patient
            }

            // Check if assigner is a doctor (RoleId == 2) or attorney (RoleId == 5) and is active
            var assigner = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == doctorId && (u.RoleId == 2 || u.RoleId == 5) && u.IsActive);

            if (assigner == null)
            {
                return false; // Invalid doctor or attorney
            }

            var assignment = new UserAssignment
            {
                AssignerId = doctorId,
                AssigneeId = patientId,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnassignPatientFromDoctorAsync(int patientId, int doctorId)
        {
            var assignment = await _context.UserAssignments
                .FirstOrDefaultAsync(ua => ua.AssigneeId == patientId && ua.AssignerId == doctorId);

            if (assignment == null)
            {
                return false; // Assignment doesn't exist
            }

            _context.UserAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<User>> GetPatientsForDoctorAsync(int doctorId)
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
            
            UserEncryptionHelper.DecryptUserData(patients, _encryptionService);
            return patients;
        }

        public async Task<List<User>> GetDoctorsForPatientAsync(int patientId)
        {
            var assigners = await _context.UserAssignments
                .Where(ua => ua.AssigneeId == patientId)
                .Include(ua => ua.Assigner)
                .ThenInclude(a => a.Role)
                .Select(ua => ua.Assigner)
                .Where(d => d.IsActive && (d.RoleId == 2 || d.RoleId == 4 || d.RoleId == 5)) // Doctors, Coordinators, and Attorneys
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();
            
            UserEncryptionHelper.DecryptUserData(assigners, _encryptionService);
            return assigners;
        }
    }
}
