using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IAdminService
    {
        Task<List<User>> GetAllDoctorsAsync();
        Task<List<User>> GetAllPatientsAsync();
        Task<List<UserAssignment>> GetUserAssignmentsAsync();
        Task<bool> AssignPatientToDoctorAsync(int patientId, int doctorId);
        Task<bool> UnassignPatientFromDoctorAsync(int patientId, int doctorId);
        Task<List<User>> GetPatientsForDoctorAsync(int doctorId);
        Task<List<User>> GetDoctorsForPatientAsync(int patientId);
    }

    public class AdminService : IAdminService
    {
        private readonly JournalDbContext _context;

        public AdminService(JournalDbContext context)
        {
            _context = context;
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

        public async Task<List<User>> GetAllPatientsAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RoleId == 1 && u.IsActive) // Role 1 = Patient
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
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

            // Check if patient and doctor exist and are active
            var patient = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == patientId && u.RoleId == 1 && u.IsActive);
            var doctor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == doctorId && u.RoleId == 2 && u.IsActive);

            if (patient == null || doctor == null)
            {
                return false; // Invalid patient or doctor
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

        public async Task<List<User>> GetDoctorsForPatientAsync(int patientId)
        {
            return await _context.UserAssignments
                .Where(ua => ua.AssigneeId == patientId)
                .Include(ua => ua.Assigner)
                .ThenInclude(a => a.Role)
                .Select(ua => ua.Assigner)
                .Where(d => d.IsActive)
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();
        }
    }
}
