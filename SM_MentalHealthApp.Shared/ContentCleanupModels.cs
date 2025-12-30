namespace SM_MentalHealthApp.Shared
{
    public class ContentCleanupResult
    {
        public int TotalDeletedContentFound { get; set; }
        public int SuccessfullyDeletedFromS3 { get; set; }
        public int FailedToDeleteFromS3 { get; set; }
        public int AlreadyDeletedFromS3 { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}

