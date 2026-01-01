namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Result of the auto-complete Service Requests job
    /// </summary>
    public class AutoCompleteServiceRequestsResult
    {
        public bool Success { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public int TotalServiceRequestsChecked { get; set; }
        public int CompletedServiceRequests { get; set; }
        public int PendingServiceRequests { get; set; }
        public int SkippedNoAssignments { get; set; }
        public List<int> CompletedServiceRequestIds { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
}

