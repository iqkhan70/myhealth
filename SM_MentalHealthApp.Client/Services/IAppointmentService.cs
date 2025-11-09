using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services;

public interface IAppointmentService
{
    Task<List<AppointmentDto>> ListAsync(int? doctorId = null, int? patientId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);
    Task<AppointmentDto?> GetAsync(int id, CancellationToken ct = default);
    Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken ct = default);
    Task<AppointmentDto> UpdateAsync(int id, UpdateAppointmentRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
    Task<AppointmentValidationResult> ValidateAsync(CreateAppointmentRequest request, CancellationToken ct = default);
    Task<List<DoctorAvailabilityDto>> GetDoctorAvailabilitiesAsync(int? doctorId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);
    Task<DoctorAvailabilityDto> SetDoctorAvailabilityAsync(DoctorAvailabilityRequest request, CancellationToken ct = default);
}

