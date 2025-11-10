namespace SM_MentalHealthApp.Shared.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class AppConstants
{
    public static class Timezones
    {
        public const string Default = "America/New_York";
        
        public static readonly List<TimezoneOption> All = new()
        {
            new("America/New_York", "Eastern Time (ET) - America/New_York"),
            new("America/Chicago", "Central Time (CT) - America/Chicago"),
            new("America/Denver", "Mountain Time (MT) - America/Denver"),
            new("America/Los_Angeles", "Pacific Time (PT) - America/Los_Angeles"),
            new("America/Phoenix", "Arizona Time - America/Phoenix"),
            new("America/Anchorage", "Alaska Time - America/Anchorage"),
            new("Pacific/Honolulu", "Hawaii Time - Pacific/Honolulu"),
            new("UTC", "Coordinated Universal Time (UTC)"),
            new("Europe/London", "Greenwich Mean Time (GMT) - Europe/London"),
            new("Europe/Paris", "Central European Time (CET) - Europe/Paris"),
            new("Asia/Tokyo", "Japan Standard Time (JST) - Asia/Tokyo"),
            new("Asia/Shanghai", "China Standard Time (CST) - Asia/Shanghai"),
            new("Asia/Dubai", "Gulf Standard Time (GST) - Asia/Dubai"),
            new("Asia/Kolkata", "India Standard Time (IST) - Asia/Kolkata"),
            new("Australia/Sydney", "Australian Eastern Time (AET) - Australia/Sydney"),
            new("America/Toronto", "Eastern Time (ET) - America/Toronto"),
            new("America/Vancouver", "Pacific Time (PT) - America/Vancouver"),
            new("America/Mexico_City", "Central Time (CT) - America/Mexico_City"),
            new("America/Sao_Paulo", "Brasilia Time - America/Sao_Paulo"),
            new("Europe/Berlin", "Central European Time (CET) - Europe/Berlin")
        };
    }
    
    public static class AppointmentTypes
    {
        public const string Regular = "Regular";
        public const string UrgentCare = "Urgent Care";
    }
    
    public static class Validation
    {
        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 128;
        public const int MaxEmailLength = 255;
        public const int MaxNameLength = 100;
    }
}

public class TimezoneOption
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    
    public TimezoneOption() { }
    
    public TimezoneOption(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }
}

