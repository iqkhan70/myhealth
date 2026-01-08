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
        Task<List<User>> GetAllSmesAsync();
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
        private readonly IServiceRequestService? _serviceRequestService;

        public AdminService(JournalDbContext context, IPiiEncryptionService encryptionService, IServiceRequestService? serviceRequestService = null)
        {
            _context = context;
            _encryptionService = encryptionService;
            _serviceRequestService = serviceRequestService;
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
                .Where(u => (u.RoleId == 5 || u.RoleId == 6) && u.IsActive) // Role 5 = Attorney, Role 6 = SME
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            UserEncryptionHelper.DecryptUserData(attorneys, _encryptionService);
            return attorneys;
        }

        public async Task<List<User>> GetAllSmesAsync()
        {
            var smes = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RoleId == 6 && u.IsActive) // Role 6 = SME
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            UserEncryptionHelper.DecryptUserData(smes, _encryptionService);
            return smes;
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
                System.Diagnostics.Debug.WriteLine($"Assignment already exists: PatientId={patientId}, DoctorId={doctorId}");
                return false; // Already assigned
            }

            // Check if patient exists and is active
            var patient = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == patientId && u.RoleId == 1 && u.IsActive);

            if (patient == null)
            {
                System.Diagnostics.Debug.WriteLine($"Patient not found or invalid: PatientId={patientId}, RoleId should be 1, IsActive should be true");
                // Check if user exists but wrong role or inactive
                var userCheck = await _context.Users.FirstOrDefaultAsync(u => u.Id == patientId);
                if (userCheck != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User exists but RoleId={userCheck.RoleId}, IsActive={userCheck.IsActive}");
                }
                return false; // Invalid patient
            }

            // Check if assigner is a doctor (RoleId == 2), attorney (RoleId == 5), or SME (RoleId == 6) and is active
            var assigner = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == doctorId && (u.RoleId == 2 || u.RoleId == 5 || u.RoleId == 6) && u.IsActive);

            if (assigner == null)
            {
                System.Diagnostics.Debug.WriteLine($"Assigner not found or invalid: DoctorId={doctorId}, Must be RoleId 2, 5, or 6 and IsActive=true");
                // Check if user exists but wrong role or inactive
                var userCheck = await _context.Users.FirstOrDefaultAsync(u => u.Id == doctorId);
                if (userCheck != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User exists but RoleId={userCheck.RoleId}, IsActive={userCheck.IsActive}");
                }
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

            // Also assign to the patient's default "General" Service Request if it exists
            // This ensures the assignment shows up in GetDoctorsForPatientAsync which uses ServiceRequestAssignments
            if (_serviceRequestService != null)
            {
                try
                {
                    var defaultSr = await _serviceRequestService.GetDefaultServiceRequestForClientAsync(patientId);
                    if (defaultSr != null)
                    {
                        // Check if assignment already exists
                        var existingSrAssignment = await _context.ServiceRequestAssignments
                            .FirstOrDefaultAsync(a => a.ServiceRequestId == defaultSr.Id &&
                                a.SmeUserId == doctorId &&
                                a.IsActive);

                        if (existingSrAssignment == null)
                        {
                            var srAssignment = new ServiceRequestAssignment
                            {
                                ServiceRequestId = defaultSr.Id,
                                SmeUserId = doctorId,
                                AssignedAt = DateTime.UtcNow,
                                IsActive = true,
                                Status = AssignmentStatus.Assigned.ToString(), // Use enum
                                IsBillable = false, // Not billable until work starts
                                BillingStatus = BillingStatus.NotBillable.ToString() // Not billable initially
                            };
                            _context.ServiceRequestAssignments.Add(srAssignment);
                            await _context.SaveChangesAsync();
                            System.Diagnostics.Debug.WriteLine($"Successfully synced assignment to ServiceRequest {defaultSr.Id} for patient {patientId} and doctor {doctorId}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"ServiceRequest assignment already exists for SR {defaultSr.Id}, patient {patientId}, doctor {doctorId}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"No default ServiceRequest found for patient {patientId} - skipping SR sync");
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the assignment - UserAssignment was already created
                    // This is a best-effort sync to ServiceRequestAssignments
                    System.Diagnostics.Debug.WriteLine($"Failed to sync assignment to ServiceRequest: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ServiceRequestService is null - skipping SR sync for patient {patientId} and doctor {doctorId}");
            }

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

            // Also unassign from the patient's default "General" Service Request if it exists
            // This ensures the unassignment is reflected in GetDoctorsForPatientAsync
            if (_serviceRequestService != null)
            {
                try
                {
                    var defaultSr = await _serviceRequestService.GetDefaultServiceRequestForClientAsync(patientId);
                    if (defaultSr != null)
                    {
                        var srAssignment = await _context.ServiceRequestAssignments
                            .FirstOrDefaultAsync(a => a.ServiceRequestId == defaultSr.Id &&
                                a.SmeUserId == doctorId &&
                                a.IsActive);

                        if (srAssignment != null)
                        {
                            srAssignment.IsActive = false;
                            srAssignment.UnassignedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the unassignment - UserAssignment was already removed
                    System.Diagnostics.Debug.WriteLine($"Failed to sync unassignment from ServiceRequest: {ex.Message}");
                }
            }

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
            // Get all SMEs (doctors, coordinators, attorneys) assigned to this patient
            // We check both ServiceRequestAssignments (for SR-based assignments) and UserAssignments (for direct assignments)

            // Step 1: Get all ServiceRequests for this patient
            var serviceRequestIds = await _context.ServiceRequests
                .Where(sr => sr.ClientId == patientId && sr.IsActive)
                .Select(sr => sr.Id)
                .ToListAsync();

            // Step 2: Get all active assignments for these ServiceRequests
            var smeUserIdsFromSr = await _context.ServiceRequestAssignments
                .Where(a => serviceRequestIds.Contains(a.ServiceRequestId) && a.IsActive)
                .Select(a => a.SmeUserId)
                .Distinct()
                .ToListAsync();

            // Step 3: Also get assignments from UserAssignments (for assignments made via Patients page)
            var smeUserIdsFromUserAssignments = await _context.UserAssignments
                .Where(ua => ua.AssigneeId == patientId)
                .Select(ua => ua.AssignerId)
                .Distinct()
                .ToListAsync();

            // Step 4: Combine both lists and get unique SME user IDs
            var smeUserIds = smeUserIdsFromSr.Union(smeUserIdsFromUserAssignments).Distinct().ToList();

            // Step 5: Get the full User entities for these SMEs
            var assigners = await _context.Users
                .Include(u => u.Role)
                .Where(u => smeUserIds.Contains(u.Id) &&
                           u.IsActive &&
                           (u.RoleId == 2 || u.RoleId == 4 || u.RoleId == 5 || u.RoleId == 6)) // Active doctors, coordinators, attorneys, and SMEs
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();

            UserEncryptionHelper.DecryptUserData(assigners, _encryptionService);
            return assigners;
        }
    }
}
