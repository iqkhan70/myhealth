using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SM_MentalHealthApp.Shared;
using System.Linq;

namespace SM_MentalHealthApp.Server.Filters
{
    /// <summary>
    /// Result filter to map Appointment entities to AppointmentDto in OData responses
    /// </summary>
    public class ODataAppointmentMappingFilter : IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ObjectResult objectResult && objectResult.Value != null)
            {
                try
                {
                    // Check if the result contains Appointment objects
                    if (objectResult.Value is IEnumerable<Appointment> appointments)
                    {
                        var appointmentList = appointments.ToList();
                        if (appointmentList.Count > 0 && appointmentList[0] is Appointment)
                        {
                            var dtos = appointmentList.Select(MapToDto).ToList();
                            objectResult.Value = dtos;
                        }
                    }
                    else if (objectResult.Value is Appointment appointment)
                    {
                        objectResult.Value = MapToDto(appointment);
                    }
                    else if (objectResult.Value is Microsoft.AspNetCore.OData.Results.PageResult<Appointment> pageResult)
                    {
                        var dtos = pageResult.Items.Select(MapToDto).ToList();
                        objectResult.Value = new Microsoft.AspNetCore.OData.Results.PageResult<AppointmentDto>(
                            dtos,
                            pageResult.NextPageLink,
                            pageResult.Count);
                    }
                    else if (objectResult.Value is Microsoft.AspNetCore.OData.Results.SingleResult<Appointment> singleResult)
                    {
                        // SingleResult wraps a queryable, we need to materialize it
                        var singleAppointment = singleResult.Queryable.FirstOrDefault();
                        if (singleAppointment != null)
                        {
                            objectResult.Value = MapToDto(singleAppointment);
                        }
                    }
                    // Handle IQueryable<Appointment> - materialize and map
                    else if (objectResult.Value is System.Linq.IQueryable<Appointment> queryable)
                    {
                        var queryableAppointments = queryable.ToList();
                        if (queryableAppointments.Any())
                        {
                            var dtos = queryableAppointments.Select(MapToDto).ToList();
                            objectResult.Value = dtos;
                        }
                    }
                    else
                    {
                        // Try to handle dynamic/unknown types that might contain Appointments
                        var valueType = objectResult.Value.GetType();
                        if (valueType.IsGenericType)
                        {
                            var genericArgs = valueType.GetGenericArguments();
                            if (genericArgs.Length > 0 && genericArgs[0] == typeof(Appointment))
                            {
                                // Try to extract items using reflection
                                var itemsProperty = valueType.GetProperty("Items") ?? valueType.GetProperty("Value");
                                if (itemsProperty != null)
                                {
                                    var items = itemsProperty.GetValue(objectResult.Value) as IEnumerable<Appointment>;
                                    if (items != null)
                                    {
                                        var dtos = items.Select(MapToDto).ToList();
                                        objectResult.Value = dtos;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't crash - let OData handle it
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            // No action needed after result execution
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
                TimeZoneId = appointment.TimeZoneId,
                CreatedBy = appointment.CreatedByUser != null
                    ? $"{appointment.CreatedByUser.FirstName} {appointment.CreatedByUser.LastName}"
                    : "Unknown",
                CreatedAt = appointment.CreatedAt
            };
        }
    }
}

