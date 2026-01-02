using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Assignment status lifecycle
    /// </summary>
    public enum AssignmentStatus
    {
        Assigned = 0,      // Initial state when coordinator assigns
        Accepted = 1,      // SME accepted the assignment
        Rejected = 2,      // SME rejected the assignment
        InProgress = 3,    // SME started working on it
        Completed = 4,     // Assignment completed successfully
        Abandoned = 5      // Assignment abandoned (never completed)
    }

    /// <summary>
    /// Reasons for assignment outcome
    /// </summary>
    public enum OutcomeReason
    {
        None = 0,
        SME_NoResponse = 1,
        SME_Rejected = 2,
        SME_Overloaded = 3,
        SME_Conflict = 4,
        SME_OutOfScope = 5,
        Client_NoResponse = 6,
        Client_Cancelled = 7,
        Client_Unavailable = 8,
        Coordinator_Cancelled = 9,
        System_Error = 10,
        Other = 11
    }

    /// <summary>
    /// Party responsible for the assignment outcome
    /// </summary>
    public enum ResponsibilityParty
    {
        Unknown = 0,
        SME = 1,
        Client = 2,
        System = 3,
        Coordinator = 4
    }

    /// <summary>
    /// Request to accept an assignment
    /// </summary>
    public class AcceptAssignmentRequest
    {
        [Required]
        public int AssignmentId { get; set; }
    }

    /// <summary>
    /// Request to reject an assignment
    /// </summary>
    public class RejectAssignmentRequest
    {
        [Required]
        public int AssignmentId { get; set; }

        [Required]
        public OutcomeReason Reason { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to update assignment status
    /// </summary>
    public class UpdateAssignmentStatusRequest
    {
        [Required]
        public int AssignmentId { get; set; }

        [Required]
        public AssignmentStatus Status { get; set; }

        public OutcomeReason? OutcomeReason { get; set; }

        public ResponsibilityParty? ResponsibilityParty { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for assignment with lifecycle information
    /// </summary>
    public class ServiceRequestAssignmentDto
    {
        public int Id { get; set; }
        public int ServiceRequestId { get; set; }
        public int SmeUserId { get; set; }
        public string SmeUserName { get; set; } = string.Empty;
        public int? SmeScore { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = "Assigned";
        public string? OutcomeReason { get; set; }
        public string? ResponsibilityParty { get; set; }
        public bool IsBillable { get; set; }
        public int? AssignedByUserId { get; set; }
        public string? AssignedByUserName { get; set; }
    }

    /// <summary>
    /// DTO for SME recommendation (used by coordinators)
    /// </summary>
    public class SmeRecommendationDto
    {
        public int SmeUserId { get; set; }
        public string SmeUserName { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public int SmeScore { get; set; }
        public int ActiveAssignmentsCount { get; set; }
        public int RecentRejectionsCount { get; set; }
        public double CompletionRate { get; set; }
        public int ExpertiseMatchCount { get; set; } // How many SR expertise tags the SME matches
        public int TotalExpertiseRequired { get; set; } // Total expertise tags on the SR
        public double? DistanceMiles { get; set; } // Distance in miles from service location (null if location unavailable)
        public string RecommendationReason { get; set; } = string.Empty;
    }
}

