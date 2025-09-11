using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IAdminService
    {
        Task<List<Doctor>> GetAllDoctorsAsync();
        Task<List<Patient>> GetAllPatientsAsync();
        Task<List<DoctorPatient>> GetDoctorPatientAssignmentsAsync();
        Task<bool> AssignPatientToDoctorAsync(int patientId, int doctorId);
        Task<bool> UnassignPatientFromDoctorAsync(int patientId, int doctorId);
        Task<List<Patient>> GetPatientsForDoctorAsync(int doctorId);
        Task<List<Doctor>> GetDoctorsForPatientAsync(int patientId);
    }

    public class AdminService : IAdminService
    {
        private readonly JournalDbContext _context;

        public AdminService(JournalDbContext context)
        {
            _context = context;
        }

        public async Task<List<Doctor>> GetAllDoctorsAsync()
        {
            return await _context.Doctors
                .Include(d => d.Role)
                .Where(d => d.IsActive)
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();
        }

        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            return await _context.Patients
                .Include(p => p.Role)
                .Where(p => p.IsActive)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
        }

        public async Task<List<DoctorPatient>> GetDoctorPatientAssignmentsAsync()
        {
            return await _context.DoctorPatients
                .Include(dp => dp.Doctor)
                .Include(dp => dp.Patient)
                .ToListAsync();
        }

        public async Task<bool> AssignPatientToDoctorAsync(int patientId, int doctorId)
        {
            // Check if assignment already exists
            var existingAssignment = await _context.DoctorPatients
                .FirstOrDefaultAsync(dp => dp.PatientId == patientId && dp.DoctorId == doctorId);

            if (existingAssignment != null)
            {
                return false; // Already assigned
            }

            // Check if patient and doctor exist and are active
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive);
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == doctorId && d.IsActive);

            if (patient == null || doctor == null)
            {
                return false; // Invalid patient or doctor
            }

            var assignment = new DoctorPatient
            {
                DoctorId = doctorId,
                PatientId = patientId,
                AssignedAt = DateTime.UtcNow
            };

            _context.DoctorPatients.Add(assignment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnassignPatientFromDoctorAsync(int patientId, int doctorId)
        {
            var assignment = await _context.DoctorPatients
                .FirstOrDefaultAsync(dp => dp.PatientId == patientId && dp.DoctorId == doctorId);

            if (assignment == null)
            {
                return false; // Assignment doesn't exist
            }

            _context.DoctorPatients.Remove(assignment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Patient>> GetPatientsForDoctorAsync(int doctorId)
        {
            return await _context.DoctorPatients
                .Where(dp => dp.DoctorId == doctorId)
                .Include(dp => dp.Patient)
                .ThenInclude(p => p.Role)
                .Select(dp => dp.Patient)
                .Where(p => p.IsActive)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
        }

        public async Task<List<Doctor>> GetDoctorsForPatientAsync(int patientId)
        {
            return await _context.DoctorPatients
                .Where(dp => dp.PatientId == patientId)
                .Include(dp => dp.Doctor)
                .ThenInclude(d => d.Role)
                .Select(dp => dp.Doctor)
                .Where(d => d.IsActive)
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();
        }
    }
}
