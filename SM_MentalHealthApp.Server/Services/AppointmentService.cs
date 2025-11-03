using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IAppointmentService
    {
        Task<AppointmentValidationResult> ValidateAppointmentAsync(CreateAppointmentRequest request);
        Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request, int createdByUserId);
        Task<AppointmentDto?> UpdateAppointmentAsync(UpdateAppointmentRequest request);
        Task<bool> CancelAppointmentAsync(int appointmentId);
        Task<List<AppointmentDto>> GetAppointmentsAsync(int? doctorId = null, int? patientId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<AppointmentDto?> GetAppointmentByIdAsync(int appointmentId);
        Task<AppointmentConflictCheck> CheckConflictsAsync(int doctorId, DateTime startDateTime, DateTime endDateTime, int? excludeAppointmentId = null);
        Task<bool> IsPatientAssignedToDoctorAsync(int patientId, int doctorId);
        Task<DoctorAvailability?> GetDoctorAvailabilityAsync(int doctorId, DateTime date);
        Task<DoctorAvailability> SetDoctorAvailabilityAsync(DoctorAvailabilityRequest request);
        Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime dateTime);
        Task<bool> IsBusinessHoursAsync(DateTime dateTime);
    }

    public class AppointmentService : IAppointmentService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<AppointmentService> _logger;
        private readonly BusinessHours _businessHours;

        public AppointmentService(JournalDbContext context, ILogger<AppointmentService> logger)
        {
            _context = context;
            _logger = logger;
            _businessHours = new BusinessHours(); // Default: 9 AM - 5 PM, Mon-Fri
        }

        public async Task<AppointmentValidationResult> ValidateAppointmentAsync(CreateAppointmentRequest request)
        {
            var result = new AppointmentValidationResult { IsValid = true };

            // Check if patient is assigned to doctor
            var isAssigned = await IsPatientAssignedToDoctorAsync(request.PatientId, request.DoctorId);
            if (!isAssigned)
            {
                result.IsValid = false;
                result.ErrorMessage = "Patient must be assigned to this doctor before scheduling an appointment.";
                return result;
            }

            // Check if doctor exists and is active
            var doctor = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == request.DoctorId && u.IsActive);

            if (doctor == null || doctor.RoleId != 2) // Role 2 = Doctor
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid or inactive doctor.";
                return result;
            }

            // Check if patient exists and is active
            var patient = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.PatientId && u.IsActive);

            if (patient == null)
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid or inactive patient.";
                return result;
            }

            // Check if appointment is in the past
            if (request.AppointmentDateTime < DateTime.UtcNow)
            {
                result.IsValid = false;
                result.ErrorMessage = "Cannot schedule appointments in the past.";
                return result;
            }

            var appointmentDate = request.AppointmentDateTime.Date;
            var appointmentTime = request.AppointmentDateTime.TimeOfDay;
            var endDateTime = request.AppointmentDateTime.Add(request.Duration);

            // Check doctor availability (OOO check)
            var availability = await GetDoctorAvailabilityAsync(request.DoctorId, appointmentDate);
            if (availability != null && availability.IsOutOfOffice)
            {
                // Check if it's full day OOO or partial
                if (!availability.StartTime.HasValue || !availability.EndTime.HasValue)
                {
                    // Full day OOO
                    result.IsValid = false;
                    result.ErrorMessage = $"Doctor is out of office on {appointmentDate:MM/dd/yyyy}. {(string.IsNullOrEmpty(availability.Reason) ? "" : $"Reason: {availability.Reason}")}";
                    return result;
                }
                else
                {
                    // Partial day OOO - check if appointment falls in OOO time
                    if (appointmentTime >= availability.StartTime.Value && appointmentTime < availability.EndTime.Value)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"Doctor is unavailable during this time. {(string.IsNullOrEmpty(availability.Reason) ? "" : $"Reason: {availability.Reason}")}";
                        return result;
                    }
                }
            }

            // Check for overlapping appointments
            var conflictCheck = await CheckConflictsAsync(request.DoctorId, request.AppointmentDateTime, endDateTime);
            if (conflictCheck.HasConflict)
            {
                result.IsValid = false;
                result.ErrorMessage = conflictCheck.ConflictMessage ?? "Appointment conflicts with existing appointments.";
                return result;
            }

            // Check if it's business hours (for regular appointments)
            var isBusinessHours = await IsBusinessHoursAsync(request.AppointmentDateTime);
            if (request.AppointmentType == AppointmentType.Regular && !isBusinessHours)
            {
                result.Warnings.Add("This appointment is scheduled outside business hours. Consider marking it as Urgent Care if appropriate.");
            }

            return result;
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request, int createdByUserId)
        {
            // Validate first
            var validation = await ValidateAppointmentAsync(request);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.ErrorMessage);
            }

            var isBusinessHours = await IsBusinessHoursAsync(request.AppointmentDateTime);

            var appointment = new Appointment
            {
                DoctorId = request.DoctorId,
                PatientId = request.PatientId,
                AppointmentDateTime = request.AppointmentDateTime,
                Duration = request.Duration,
                AppointmentType = request.AppointmentType,
                Status = AppointmentStatus.Scheduled,
                Reason = request.Reason,
                Notes = request.Notes,
                CreatedByUserId = createdByUserId,
                IsBusinessHours = isBusinessHours,
                IsActive = true
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointment.Id) ?? throw new Exception("Failed to retrieve created appointment");
        }

        public async Task<AppointmentDto?> UpdateAppointmentAsync(UpdateAppointmentRequest request)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.CreatedByUser)
                .FirstOrDefaultAsync(a => a.Id == request.Id && a.IsActive);

            if (appointment == null)
                return null;

            // If date/time is being changed, validate
            if (request.AppointmentDateTime.HasValue)
            {
                var createRequest = new CreateAppointmentRequest
                {
                    DoctorId = appointment.DoctorId,
                    PatientId = appointment.PatientId,
                    AppointmentDateTime = request.AppointmentDateTime.Value,
                    Duration = request.Duration ?? appointment.Duration,
                    AppointmentType = request.AppointmentType ?? appointment.AppointmentType,
                    Reason = request.Reason ?? appointment.Reason,
                    Notes = request.Notes ?? appointment.Notes
                };

                var validation = await ValidateAppointmentAsync(createRequest);
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException(validation.ErrorMessage);
                }

                appointment.AppointmentDateTime = request.AppointmentDateTime.Value;
                appointment.IsBusinessHours = await IsBusinessHoursAsync(request.AppointmentDateTime.Value);
            }

            if (request.Duration.HasValue)
                appointment.Duration = request.Duration.Value;

            if (request.AppointmentType.HasValue)
                appointment.AppointmentType = request.AppointmentType.Value;

            if (request.Status.HasValue)
                appointment.Status = request.Status.Value;

            if (request.Reason != null)
                appointment.Reason = request.Reason;

            if (request.Notes != null)
                appointment.Notes = request.Notes;

            appointment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointment.Id);
        }

        public async Task<bool> CancelAppointmentAsync(int appointmentId)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.IsActive);

            if (appointment == null)
                return false;

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<AppointmentDto>> GetAppointmentsAsync(int? doctorId = null, int? patientId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.CreatedByUser)
                .Where(a => a.IsActive);

            if (doctorId.HasValue)
                query = query.Where(a => a.DoctorId == doctorId.Value);

            if (patientId.HasValue)
                query = query.Where(a => a.PatientId == patientId.Value);

            if (startDate.HasValue)
                query = query.Where(a => a.AppointmentDateTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.AppointmentDateTime <= endDate.Value);

            var appointments = await query
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();

            return appointments.Select(a => MapToDto(a)).ToList();
        }

        public async Task<AppointmentDto?> GetAppointmentByIdAsync(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.CreatedByUser)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.IsActive);

            return appointment == null ? null : MapToDto(appointment);
        }

        public async Task<AppointmentConflictCheck> CheckConflictsAsync(int doctorId, DateTime startDateTime, DateTime endDateTime, int? excludeAppointmentId = null)
        {
            // Load appointments first, then check conflicts in memory since EndDateTime is a computed property
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.CreatedByUser)
                .Where(a => a.DoctorId == doctorId
                    && a.IsActive
                    && a.Status != AppointmentStatus.Cancelled
                    && a.Status != AppointmentStatus.NoShow
                    && (excludeAppointmentId == null || a.Id != excludeAppointmentId))
                .ToListAsync();

            // Check for conflicts by calculating end times
            var conflicting = appointments.Where(a =>
            {
                var existingEndTime = a.AppointmentDateTime.Add(a.Duration);
                return (
                    // New appointment starts during existing appointment
                    (startDateTime >= a.AppointmentDateTime && startDateTime < existingEndTime) ||
                    // New appointment ends during existing appointment
                    (endDateTime > a.AppointmentDateTime && endDateTime <= existingEndTime) ||
                    // New appointment completely contains existing appointment
                    (startDateTime <= a.AppointmentDateTime && endDateTime >= existingEndTime)
                );
            }).ToList();

            var result = new AppointmentConflictCheck
            {
                HasConflict = conflicting.Any(),
                ConflictingAppointments = conflicting.Select(a => MapToDto(a)).ToList()
            };

            if (result.HasConflict)
            {
                var conflicts = string.Join(", ", conflicting.Select(a =>
                {
                    var patientName = a.Patient != null
                        ? $"{a.Patient.FirstName} {a.Patient.LastName}"
                        : $"Patient ID {a.PatientId}";
                    return $"{patientName} at {a.AppointmentDateTime:MM/dd/yyyy HH:mm}";
                }));
                result.ConflictMessage = $"This appointment slot conflicts with an existing appointment: {conflicts}";
            }

            return result;
        }

        public async Task<bool> IsPatientAssignedToDoctorAsync(int patientId, int doctorId)
        {
            return await _context.UserAssignments
                .AnyAsync(ua => ua.AssignerId == doctorId
                    && ua.AssigneeId == patientId
                    && ua.IsActive);
        }

        public async Task<DoctorAvailability?> GetDoctorAvailabilityAsync(int doctorId, DateTime date)
        {
            return await _context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == doctorId && da.Date.Date == date.Date);
        }

        public async Task<DoctorAvailability> SetDoctorAvailabilityAsync(DoctorAvailabilityRequest request)
        {
            var availability = await _context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == request.DoctorId && da.Date.Date == request.Date.Date);

            if (availability == null)
            {
                availability = new DoctorAvailability
                {
                    DoctorId = request.DoctorId,
                    Date = request.Date.Date,
                    IsOutOfOffice = request.IsOutOfOffice,
                    Reason = request.Reason,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime
                };
                _context.DoctorAvailabilities.Add(availability);
            }
            else
            {
                availability.IsOutOfOffice = request.IsOutOfOffice;
                availability.Reason = request.Reason;
                availability.StartTime = request.StartTime;
                availability.EndTime = request.EndTime;
                availability.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return availability;
        }

        public async Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime dateTime)
        {
            var availability = await GetDoctorAvailabilityAsync(doctorId, dateTime.Date);

            if (availability == null)
                return true; // No availability record means available

            if (availability.IsOutOfOffice)
            {
                // If partial day OOO, check if time falls outside OOO period
                if (availability.StartTime.HasValue && availability.EndTime.HasValue)
                {
                    var time = dateTime.TimeOfDay;
                    return time < availability.StartTime.Value || time >= availability.EndTime.Value;
                }
                return false; // Full day OOO
            }

            return true;
        }

        public async Task<bool> IsBusinessHoursAsync(DateTime dateTime)
        {
            // Check if it's a working day
            if (!_businessHours.WorkingDays.Contains(dateTime.DayOfWeek))
                return false;

            // Check if time is within business hours
            var time = dateTime.TimeOfDay;
            return time >= _businessHours.StartTime && time < _businessHours.EndTime;
        }

        private AppointmentDto MapToDto(Appointment appointment)
        {
            return new AppointmentDto
            {
                Id = appointment.Id,
                DoctorId = appointment.DoctorId,
                DoctorName = appointment.Doctor != null
                    ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
                    : "Unknown Doctor",
                DoctorEmail = appointment.Doctor?.Email ?? "",
                PatientId = appointment.PatientId,
                PatientName = appointment.Patient != null
                    ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}"
                    : "Unknown Patient",
                PatientEmail = appointment.Patient?.Email ?? "",
                AppointmentDateTime = appointment.AppointmentDateTime,
                EndDateTime = appointment.EndDateTime,
                Duration = appointment.Duration,
                AppointmentType = appointment.AppointmentType,
                Status = appointment.Status,
                Reason = appointment.Reason,
                Notes = appointment.Notes,
                IsUrgentCare = appointment.IsUrgentCare,
                IsBusinessHours = appointment.IsBusinessHours,
                CreatedBy = appointment.CreatedByUser != null
                    ? $"{appointment.CreatedByUser.FirstName} {appointment.CreatedByUser.LastName}"
                    : "Unknown",
                CreatedAt = appointment.CreatedAt
            };
        }
    }
}
