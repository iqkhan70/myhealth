namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Context object containing all data needed to generate a response
    /// </summary>
    public class ResponseContext
    {
        public string FullText { get; set; } = string.Empty;
        public string PatientDataText { get; set; } = string.Empty;
        public bool HasCriticalValues { get; set; }
        public bool HasAbnormalValues { get; set; }
        public bool HasNormalValues { get; set; }
        public bool HasAnyConcerns { get; set; }
        public bool HasMedicalData { get; set; }
        public bool HasJournalEntries { get; set; }
        public bool IsAiHealthCheck { get; set; }
        public List<string> CriticalAlerts { get; set; } = new();
        public List<string> NormalValues { get; set; } = new();
        public List<string> AbnormalValues { get; set; } = new();
        public List<string> JournalEntries { get; set; } = new();
    }
}

