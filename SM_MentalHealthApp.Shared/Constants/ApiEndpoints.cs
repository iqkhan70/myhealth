namespace SM_MentalHealthApp.Shared.Constants;

/// <summary>
/// Centralized API endpoint constants to avoid magic strings
/// </summary>
public static class ApiEndpoints
{
    public const string Base = "api";
    
    public static class Auth
    {
        public const string Login = "api/auth/login";
        public const string Register = "api/auth/register";
        public const string ChangePassword = "api/auth/change-password";
    }
    
    public static class Appointments
    {
        public const string Base = "api/appointment";
        public const string Validate = "api/appointment/validate";
        public const string Availability = "api/appointment/availability";
        public const string Cancel = "api/appointment/{id}/cancel";
    }
    
    public static class Patients
    {
        public const string Base = "api/admin/patients";
        public const string Create = "api/admin/create-patient";
        public const string Update = "api/admin/update-patient/{id}";
        public const string Stats = "api/user/{id}/stats";
        public const string AiHealthCheck = "api/admin/ai-health-check/{id}";
    }
    
    public static class Doctors
    {
        public const string Base = "api/admin/doctors";
        public const string Create = "api/admin/create-doctor";
        public const string Update = "api/admin/update-doctor/{id}";
        public const string MyPatients = "api/doctor/my-patients";
        public const string AssignPatient = "api/doctor/assign-patient";
        public const string UnassignPatient = "api/doctor/unassign-patient";
    }
    
    public static class ClinicalNotes
    {
        public const string Base = "api/clinicalnotes";
        public const string NoteTypes = "api/clinicalnotes/note-types";
        public const string Priorities = "api/clinicalnotes/priorities";
    }
    
    public static class Chat
    {
        public const string Send = "api/chat/send";
        public const string History = "api/chathistory/sessions";
    }
    
    public static class Content
    {
        public const string Base = "api/content";
        public const string Upload = "api/content/upload";
    }
    
    public static class Journal
    {
        public const string Base = "api/journal";
        public const string UserEntries = "api/journal/user/{userId}";
    }
}

