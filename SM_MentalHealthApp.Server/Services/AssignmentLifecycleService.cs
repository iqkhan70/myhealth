using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IAssignmentLifecycleService
    {
        Task<bool> AcceptAssignmentAsync(int assignmentId, int smeUserId);
        Task<bool> RejectAssignmentAsync(int assignmentId, int smeUserId, OutcomeReason reason, string? notes = null);
        Task<bool> StartAssignmentAsync(int assignmentId, int smeUserId);
        Task<bool> CompleteAssignmentAsync(int assignmentId, int smeUserId);
        Task<bool> AbandonAssignmentAsync(int assignmentId, OutcomeReason reason, ResponsibilityParty responsibilityParty, string? notes = null);
        Task<bool> UpdateAssignmentStatusAsync(int assignmentId, AssignmentStatus status, OutcomeReason? outcomeReason = null, ResponsibilityParty? responsibilityParty = null, string? notes = null);
        Task<bool> AdminOverrideAssignmentStatusAsync(int assignmentId, AssignmentStatus status, OutcomeReason? outcomeReason = null, ResponsibilityParty? responsibilityParty = null, string? notes = null, int? adminUserId = null);
        Task<List<SmeRecommendationDto>> GetSmeRecommendationsAsync(int serviceRequestId, string? specialization = null);
        Task<int> GetSmeScoreAsync(int smeUserId);
        Task UpdateSmeScoreAsync(int smeUserId, int scoreChange, string reason);
    }

    public class AssignmentLifecycleService : IAssignmentLifecycleService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<AssignmentLifecycleService> _logger;

        // Score impact constants
        private const int SCORE_REJECT_NO_REASON = -5;
        private const int SCORE_NO_RESPONSE_SLA = -10;
        private const int SCORE_ABANDONED_AFTER_ACCEPT = -15;
        private const int SCORE_CLIENT_COMPLAINT = -20;
        private const int SCORE_ACCEPT_AND_COMPLETE = +3;
        private const int SCORE_COMPLETE_WITHIN_SLA = +5;
        private const int SCORE_CLIENT_POSITIVE_FEEDBACK = +10;

        private const int SCORE_MIN = 0;
        private const int SCORE_MAX = 150;

        public AssignmentLifecycleService(JournalDbContext context, ILogger<AssignmentLifecycleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// SME accepts an assignment
        /// </summary>
        public async Task<bool> AcceptAssignmentAsync(int assignmentId, int smeUserId)
        {
            try
            {
                var assignment = await _context.ServiceRequestAssignments
                    .FirstOrDefaultAsync(a => a.Id == assignmentId && a.SmeUserId == smeUserId && a.IsActive);

                if (assignment == null)
                    return false;

                if (assignment.Status != "Assigned")
                {
                    _logger.LogWarning("Assignment {AssignmentId} cannot be accepted. Current status: {Status}", assignmentId, assignment.Status);
                    return false;
                }

                assignment.Status = AssignmentStatus.Accepted.ToString();
                assignment.AcceptedAt = DateTime.UtcNow;
                assignment.OutcomeReason = null;
                assignment.ResponsibilityParty = null;
                // Set as billable when accepted (user requested billing on acceptance)
                assignment.IsBillable = true;
                assignment.BillingStatus = BillingStatus.Ready.ToString(); // Ready to be invoiced

                await _context.SaveChangesAsync();

                _logger.LogInformation("Assignment {AssignmentId} accepted by SME {SmeUserId}", assignmentId, smeUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        /// <summary>
        /// SME rejects an assignment
        /// </summary>
        public async Task<bool> RejectAssignmentAsync(int assignmentId, int smeUserId, OutcomeReason reason, string? notes = null)
        {
            try
            {
                var assignment = await _context.ServiceRequestAssignments
                    .FirstOrDefaultAsync(a => a.Id == assignmentId && a.SmeUserId == smeUserId && a.IsActive);

                if (assignment == null)
                    return false;

                if (assignment.Status != "Assigned")
                {
                    _logger.LogWarning("Assignment {AssignmentId} cannot be rejected. Current status: {Status}", assignmentId, assignment.Status);
                    return false;
                }

                assignment.Status = AssignmentStatus.Rejected.ToString();
                assignment.OutcomeReason = reason.ToString();
                assignment.ResponsibilityParty = ResponsibilityParty.SME.ToString();
                assignment.IsBillable = false;
                assignment.BillingStatus = BillingStatus.NotBillable.ToString();

                await _context.SaveChangesAsync();

                // Apply score penalty if reason is not legitimate
                bool isLegitimateReason = reason == OutcomeReason.SME_Overloaded ||
                                         reason == OutcomeReason.SME_Conflict ||
                                         reason == OutcomeReason.SME_OutOfScope;

                if (!isLegitimateReason)
                {
                    await UpdateSmeScoreAsync(smeUserId, SCORE_REJECT_NO_REASON, $"Rejected assignment {assignmentId} with reason: {reason}");
                }

                _logger.LogInformation("Assignment {AssignmentId} rejected by SME {SmeUserId}. Reason: {Reason}", assignmentId, smeUserId, reason);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        /// <summary>
        /// SME starts working on an assignment (transitions to InProgress)
        /// </summary>
        public async Task<bool> StartAssignmentAsync(int assignmentId, int smeUserId)
        {
            try
            {
                var assignment = await _context.ServiceRequestAssignments
                    .FirstOrDefaultAsync(a => a.Id == assignmentId && a.SmeUserId == smeUserId && a.IsActive);

                if (assignment == null)
                    return false;

                if (assignment.Status != AssignmentStatus.Accepted.ToString())
                {
                    _logger.LogWarning("Assignment {AssignmentId} cannot be started. Current status: {Status}", assignmentId, assignment.Status);
                    return false;
                }

                assignment.Status = AssignmentStatus.InProgress.ToString();
                assignment.StartedAt = DateTime.UtcNow;
                assignment.IsBillable = true; // Mark as billable when work starts
                assignment.BillingStatus = BillingStatus.Ready.ToString(); // Ready to be invoiced

                await _context.SaveChangesAsync();

                _logger.LogInformation("Assignment {AssignmentId} started by SME {SmeUserId}", assignmentId, smeUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        /// <summary>
        /// SME completes an assignment
        /// </summary>
        public async Task<bool> CompleteAssignmentAsync(int assignmentId, int smeUserId)
        {
            try
            {
                var assignment = await _context.ServiceRequestAssignments
                    .FirstOrDefaultAsync(a => a.Id == assignmentId && a.SmeUserId == smeUserId && a.IsActive);

                if (assignment == null)
                    return false;

                if (assignment.Status != AssignmentStatus.InProgress.ToString())
                {
                    _logger.LogWarning("Assignment {AssignmentId} cannot be completed. Current status: {Status}", assignmentId, assignment.Status);
                    return false;
                }

                assignment.Status = AssignmentStatus.Completed.ToString();
                assignment.CompletedAt = DateTime.UtcNow;
                assignment.IsBillable = true; // Ensure it's billable
                // Only set BillingStatus to Ready if not already Invoiced or Paid
                if (assignment.BillingStatus != BillingStatus.Invoiced.ToString() && 
                    assignment.BillingStatus != BillingStatus.Paid.ToString())
                {
                    assignment.BillingStatus = BillingStatus.Ready.ToString();
                }

                await _context.SaveChangesAsync();

                // Apply positive score
                await UpdateSmeScoreAsync(smeUserId, SCORE_ACCEPT_AND_COMPLETE, $"Completed assignment {assignmentId}");

                // Check if completed within SLA (e.g., within 7 days of acceptance)
                if (assignment.AcceptedAt.HasValue)
                {
                    var daysToComplete = (DateTime.UtcNow - assignment.AcceptedAt.Value).TotalDays;
                    if (daysToComplete <= 7)
                    {
                        await UpdateSmeScoreAsync(smeUserId, SCORE_COMPLETE_WITHIN_SLA, $"Completed assignment {assignmentId} within SLA");
                    }
                }

                _logger.LogInformation("Assignment {AssignmentId} completed by SME {SmeUserId}", assignmentId, smeUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        /// <summary>
        /// Mark assignment as abandoned
        /// </summary>
        public async Task<bool> AbandonAssignmentAsync(int assignmentId, OutcomeReason reason, ResponsibilityParty responsibilityParty, string? notes = null)
        {
            try
            {
                var assignment = await _context.ServiceRequestAssignments
                    .FirstOrDefaultAsync(a => a.Id == assignmentId && a.IsActive);

                if (assignment == null)
                    return false;

                assignment.Status = AssignmentStatus.Abandoned.ToString();
                assignment.OutcomeReason = reason.ToString();
                assignment.ResponsibilityParty = responsibilityParty.ToString();
                assignment.IsBillable = false;
                assignment.BillingStatus = BillingStatus.NotBillable.ToString();

                await _context.SaveChangesAsync();

                // Apply score penalty if SME is responsible and had accepted
                if (responsibilityParty == ResponsibilityParty.SME && 
                    assignment.Status == AssignmentStatus.Accepted.ToString())
                {
                    await UpdateSmeScoreAsync(assignment.SmeUserId, SCORE_ABANDONED_AFTER_ACCEPT, $"Abandoned assignment {assignmentId}");
                }

                _logger.LogInformation("Assignment {AssignmentId} abandoned. Reason: {Reason}, Responsibility: {Responsibility}", 
                    assignmentId, reason, responsibilityParty);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error abandoning assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        /// <summary>
        /// Update assignment status (generic method for coordinators/admins)
        /// </summary>
        public async Task<bool> UpdateAssignmentStatusAsync(int assignmentId, AssignmentStatus status, OutcomeReason? outcomeReason = null, ResponsibilityParty? responsibilityParty = null, string? notes = null)
        {
            try
            {
                var assignment = await _context.ServiceRequestAssignments
                    .FirstOrDefaultAsync(a => a.Id == assignmentId && a.IsActive);

                if (assignment == null)
                    return false;

                var oldStatus = assignment.Status;
                assignment.Status = status.ToString();

                if (outcomeReason.HasValue)
                    assignment.OutcomeReason = outcomeReason.Value.ToString();

                if (responsibilityParty.HasValue)
                    assignment.ResponsibilityParty = responsibilityParty.Value.ToString();

                // Set timestamps based on status
                if (status == AssignmentStatus.Accepted && assignment.AcceptedAt == null)
                {
                    assignment.AcceptedAt = DateTime.UtcNow;
                    assignment.IsBillable = true; // Billable when accepted
                    // Only set BillingStatus to Ready if not already Invoiced or Paid
                    if (assignment.BillingStatus != BillingStatus.Invoiced.ToString() && 
                        assignment.BillingStatus != BillingStatus.Paid.ToString())
                    {
                        assignment.BillingStatus = BillingStatus.Ready.ToString();
                    }
                }
                else if (status == AssignmentStatus.InProgress && assignment.StartedAt == null)
                {
                    assignment.StartedAt = DateTime.UtcNow;
                    assignment.IsBillable = true;
                    // Only set BillingStatus to Ready if not already Invoiced or Paid
                    if (assignment.BillingStatus != BillingStatus.Invoiced.ToString() && 
                        assignment.BillingStatus != BillingStatus.Paid.ToString())
                    {
                        assignment.BillingStatus = BillingStatus.Ready.ToString();
                    }
                }
                else if (status == AssignmentStatus.Completed && assignment.CompletedAt == null)
                {
                    assignment.CompletedAt = DateTime.UtcNow;
                    assignment.IsBillable = true;
                    // Only set BillingStatus to Ready if not already Invoiced or Paid
                    if (assignment.BillingStatus != BillingStatus.Invoiced.ToString() && 
                        assignment.BillingStatus != BillingStatus.Paid.ToString())
                    {
                        assignment.BillingStatus = BillingStatus.Ready.ToString();
                    }
                }

                // Update billable flag and billing status based on status
                // IsBillable = true when status is Accepted, InProgress, or Completed
                if (status == AssignmentStatus.Accepted || status == AssignmentStatus.InProgress || status == AssignmentStatus.Completed)
                {
                    assignment.IsBillable = true;
                    // Only set BillingStatus to Ready if not already Invoiced or Paid
                    if (assignment.BillingStatus != BillingStatus.Invoiced.ToString() && 
                        assignment.BillingStatus != BillingStatus.Paid.ToString())
                    {
                        assignment.BillingStatus = BillingStatus.Ready.ToString();
                    }
                }
                else
                {
                    assignment.IsBillable = false;
                    assignment.BillingStatus = BillingStatus.NotBillable.ToString();
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Assignment {AssignmentId} status updated from {OldStatus} to {NewStatus}", 
                    assignmentId, oldStatus, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assignment {AssignmentId} status", assignmentId);
                return false;
            }
        }

        /// <summary>
        /// Admin override assignment status (for correcting mistakes, e.g., reversing completion)
        /// This allows admins to change status even if it doesn't follow normal workflow
        /// </summary>
        public async Task<bool> AdminOverrideAssignmentStatusAsync(int assignmentId, AssignmentStatus status, OutcomeReason? outcomeReason = null, ResponsibilityParty? responsibilityParty = null, string? notes = null, int? adminUserId = null)
        {
            try
            {
                var assignment = await _context.ServiceRequestAssignments
                    .FirstOrDefaultAsync(a => a.Id == assignmentId && a.IsActive);

                if (assignment == null)
                    return false;

                var oldStatus = assignment.Status;
                var oldIsBillable = assignment.IsBillable;

                // Admin can override to any status
                assignment.Status = status.ToString();

                if (outcomeReason.HasValue)
                    assignment.OutcomeReason = outcomeReason.Value.ToString();

                if (responsibilityParty.HasValue)
                    assignment.ResponsibilityParty = responsibilityParty.Value.ToString();

                // Set timestamps based on status (only if not already set)
                if (status == AssignmentStatus.Accepted && assignment.AcceptedAt == null)
                    assignment.AcceptedAt = DateTime.UtcNow;
                else if (status == AssignmentStatus.InProgress && assignment.StartedAt == null)
                {
                    assignment.StartedAt = DateTime.UtcNow;
                }
                else if (status == AssignmentStatus.Completed && assignment.CompletedAt == null)
                    assignment.CompletedAt = DateTime.UtcNow;

                // If reverting from Completed, clear CompletedAt
                if (oldStatus == AssignmentStatus.Completed.ToString() && status != AssignmentStatus.Completed)
                {
                    assignment.CompletedAt = null;
                }

                // If reverting from InProgress, clear StartedAt
                if (oldStatus == AssignmentStatus.InProgress.ToString() && status != AssignmentStatus.InProgress && status != AssignmentStatus.Completed)
                {
                    assignment.StartedAt = null;
                }

                // Update billable flag and billing status based on new status
                // Preserve Invoiced/Paid status - don't change if already invoiced or paid
                // IsBillable = true when status is Accepted, InProgress, or Completed
                if (status == AssignmentStatus.Accepted || status == AssignmentStatus.InProgress || status == AssignmentStatus.Completed)
                {
                    assignment.IsBillable = true;
                    // Only set BillingStatus to Ready if not already Invoiced or Paid
                    if (assignment.BillingStatus != BillingStatus.Invoiced.ToString() && 
                        assignment.BillingStatus != BillingStatus.Paid.ToString())
                    {
                        assignment.BillingStatus = BillingStatus.Ready.ToString();
                    }
                }
                else
                {
                    assignment.IsBillable = false;
                    // Only set to NotBillable if not already Invoiced or Paid
                    if (assignment.BillingStatus != BillingStatus.Invoiced.ToString() && 
                        assignment.BillingStatus != BillingStatus.Paid.ToString())
                    {
                        assignment.BillingStatus = BillingStatus.NotBillable.ToString();
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogWarning("ADMIN OVERRIDE: Assignment {AssignmentId} status changed from {OldStatus} to {NewStatus} by Admin {AdminUserId}. " +
                    "IsBillable changed from {OldBillable} to {NewBillable}. BillingStatus: {BillingStatus}. Notes: {Notes}", 
                    assignmentId, oldStatus, status, adminUserId, oldIsBillable, assignment.IsBillable, assignment.BillingStatus, notes);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in admin override for assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        /// <summary>
        /// Get SME recommendations for a service request (sorted by score, workload, etc.)
        /// </summary>
        public async Task<List<SmeRecommendationDto>> GetSmeRecommendationsAsync(int serviceRequestId, string? specialization = null)
        {
            try
            {
                var serviceRequest = await _context.ServiceRequests
                    .Include(sr => sr.Expertises)
                    .Include(sr => sr.Client)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId && sr.IsActive);

                if (serviceRequest == null)
                    return new List<SmeRecommendationDto>();

                // Get SR's expertise IDs
                var srExpertiseIds = serviceRequest.Expertises?.Select(e => e.ExpertiseId).ToHashSet() ?? new HashSet<int>();

                // Get service location (use ServiceZipCode if set, otherwise use client's ZipCode)
                var serviceZipCode = serviceRequest.ServiceZipCode ?? serviceRequest.Client?.ZipCode;
                var maxDistanceMiles = serviceRequest.MaxDistanceMiles;
                
                // Lookup lat/lon for service location
                (decimal? serviceLat, decimal? serviceLon) = (null, null);
                if (!string.IsNullOrEmpty(serviceZipCode))
                {
                    var zipLookup = await _context.ZipCodeLookups
                        .FirstOrDefaultAsync(z => z.ZipCode == serviceZipCode);
                    if (zipLookup != null)
                    {
                        serviceLat = zipLookup.Latitude;
                        serviceLon = zipLookup.Longitude;
                    }
                }

                // Get all active SMEs (doctors, attorneys, and SMEs) with their expertise
                var smeQuery = _context.Users
                    .Include(u => u.SmeExpertises)
                        .ThenInclude(se => se.Expertise)
                    .Where(u => u.IsActive && 
                        (u.RoleId == Shared.Constants.Roles.Doctor || u.RoleId == Shared.Constants.Roles.Attorney || u.RoleId == Shared.Constants.Roles.Sme));

                if (!string.IsNullOrEmpty(specialization))
                {
                    smeQuery = smeQuery.Where(u => u.Specialization != null && u.Specialization.Contains(specialization));
                }

                var smes = await smeQuery.ToListAsync();

                var recommendations = new List<SmeRecommendationDto>();

                foreach (var sme in smes)
                {
                    // Calculate expertise match count
                    var smeExpertiseIds = sme.SmeExpertises?
                        .Where(se => se.IsActive)
                        .Select(se => se.ExpertiseId)
                        .ToHashSet() ?? new HashSet<int>();

                    var matchCount = srExpertiseIds.Count > 0 
                        ? smeExpertiseIds.Count(smeExpId => srExpertiseIds.Contains(smeExpId))
                        : 0;

                    // Only include SMEs that match at least one expertise (if SR has expertise)
                    if (srExpertiseIds.Count > 0 && matchCount == 0)
                        continue;

                    // Calculate distance if both locations are available
                    double? distanceMiles = null;
                    if (serviceLat.HasValue && serviceLon.HasValue && sme.Latitude.HasValue && sme.Longitude.HasValue)
                    {
                        distanceMiles = CalculateDistanceMiles(
                            (double)serviceLat.Value, (double)serviceLon.Value,
                            (double)sme.Latitude.Value, (double)sme.Longitude.Value);
                        
                        // Filter by max distance if set
                        if (maxDistanceMiles > 0 && distanceMiles > maxDistanceMiles)
                        {
                            // Also check SME's MaxTravelMiles if set
                            if (sme.MaxTravelMiles.HasValue && distanceMiles > sme.MaxTravelMiles.Value)
                            {
                                continue; // Skip this SME - too far
                            }
                            else if (!sme.MaxTravelMiles.HasValue)
                            {
                                continue; // Skip this SME - exceeds SR max distance
                            }
                        }
                    }

                    // Get active assignments count
                    var activeAssignments = await _context.ServiceRequestAssignments
                        .CountAsync(a => a.SmeUserId == sme.Id && 
                            a.IsActive && 
                            (a.Status == AssignmentStatus.Accepted.ToString() || 
                             a.Status == AssignmentStatus.InProgress.ToString()));

                    // Get recent rejections (last 30 days)
                    var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                    var recentRejections = await _context.ServiceRequestAssignments
                        .CountAsync(a => a.SmeUserId == sme.Id && 
                            a.Status == AssignmentStatus.Rejected.ToString() &&
                            a.AssignedAt >= thirtyDaysAgo);

                    // Calculate completion rate (last 90 days)
                    var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
                    var totalAssignments = await _context.ServiceRequestAssignments
                        .CountAsync(a => a.SmeUserId == sme.Id && 
                            a.AssignedAt >= ninetyDaysAgo);
                    var completedAssignments = await _context.ServiceRequestAssignments
                        .CountAsync(a => a.SmeUserId == sme.Id && 
                            a.Status == AssignmentStatus.Completed.ToString() &&
                            a.AssignedAt >= ninetyDaysAgo);
                    var completionRate = totalAssignments > 0 ? (double)completedAssignments / totalAssignments : 1.0;

                    var recommendation = new SmeRecommendationDto
                    {
                        SmeUserId = sme.Id,
                        SmeUserName = $"{sme.FirstName} {sme.LastName}",
                        Specialization = sme.Specialization,
                        SmeScore = sme.SmeScore,
                        ActiveAssignmentsCount = activeAssignments,
                        RecentRejectionsCount = recentRejections,
                        CompletionRate = completionRate,
                        ExpertiseMatchCount = matchCount,
                        TotalExpertiseRequired = srExpertiseIds.Count,
                        DistanceMiles = distanceMiles,
                        RecommendationReason = BuildRecommendationReason(sme.SmeScore, activeAssignments, recentRejections, completionRate, matchCount, srExpertiseIds.Count, distanceMiles)
                    };

                    recommendations.Add(recommendation);
                }

                // Sort by: 1) Client's preferred SME (if set), 2) Match count (desc), 3) Distance (asc - closest first), 4) Highest score, 5) Lowest rejections, 6) Lowest workload
                var preferredSmeUserId = serviceRequest.PreferredSmeUserId;
                return recommendations
                    .OrderByDescending(r => preferredSmeUserId.HasValue && r.SmeUserId == preferredSmeUserId.Value) // Preferred SME first
                    .ThenByDescending(r => r.ExpertiseMatchCount)
                    .ThenBy(r => r.DistanceMiles ?? double.MaxValue) // Closest first (nulls go to end)
                    .ThenByDescending(r => r.SmeScore)
                    .ThenBy(r => r.RecentRejectionsCount)
                    .ThenBy(r => r.ActiveAssignmentsCount)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SME recommendations for service request {ServiceRequestId}", serviceRequestId);
                return new List<SmeRecommendationDto>();
            }
        }

        /// <summary>
        /// Calculate distance between two lat/lon points using Haversine formula
        /// Returns distance in miles
        /// Formula: d = 2r * arcsin(sqrt(sin²(Δφ/2) + cos(φ1) * cos(φ2) * sin²(Δλ/2)))
        /// Where: φ = latitude, λ = longitude, r = Earth's radius
        /// </summary>
        private double CalculateDistanceMiles(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadiusMiles = 3958.8; // Earth's radius in miles

            // Convert degrees to radians
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            // Haversine formula
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = EarthRadiusMiles * c;

            // Log calculation for debugging (can be removed in production)
            _logger.LogDebug("Distance calculation: ({Lat1}, {Lon1}) to ({Lat2}, {Lon2}) = {Distance} miles",
                lat1, lon1, lat2, lon2, Math.Round(distance, 1));

            return Math.Round(distance, 1); // Round to 1 decimal place
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Get current SME score
        /// </summary>
        public async Task<int> GetSmeScoreAsync(int smeUserId)
        {
            try
            {
                var sme = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == smeUserId);

                return sme?.SmeScore ?? 100;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SME score for user {SmeUserId}", smeUserId);
                return 100;
            }
        }

        /// <summary>
        /// Update SME score with clamping
        /// </summary>
        public async Task UpdateSmeScoreAsync(int smeUserId, int scoreChange, string reason)
        {
            try
            {
                var sme = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == smeUserId);

                if (sme == null)
                    return;

                var newScore = sme.SmeScore + scoreChange;
                newScore = Math.Max(SCORE_MIN, Math.Min(SCORE_MAX, newScore)); // Clamp between 0 and 150

                var oldScore = sme.SmeScore;
                sme.SmeScore = newScore;

                await _context.SaveChangesAsync();

                _logger.LogInformation("SME {SmeUserId} score updated: {OldScore} -> {NewScore} ({Change:+0;-0}). Reason: {Reason}", 
                    smeUserId, oldScore, newScore, scoreChange, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SME score for user {SmeUserId}", smeUserId);
            }
        }

        private string BuildRecommendationReason(int score, int activeAssignments, int recentRejections, double completionRate, int matchCount = 0, int totalRequired = 0, double? distanceMiles = null)
        {
            var reasons = new List<string>();

            // Add expertise match info
            if (totalRequired > 0)
            {
                if (matchCount == totalRequired)
                    reasons.Add($"Perfect match ({matchCount}/{totalRequired} expertise)");
                else if (matchCount > 0)
                    reasons.Add($"Partial match ({matchCount}/{totalRequired} expertise)");
                else
                    reasons.Add("No expertise match");
            }

            // Add distance info
            if (distanceMiles.HasValue)
            {
                if (distanceMiles <= 25)
                    reasons.Add($"Very close ({distanceMiles:F1} mi)");
                else if (distanceMiles <= 50)
                    reasons.Add($"Close ({distanceMiles:F1} mi)");
                else if (distanceMiles <= 100)
                    reasons.Add($"Moderate distance ({distanceMiles:F1} mi)");
                else
                    reasons.Add($"Far ({distanceMiles:F1} mi)");
            }

            if (score >= 120)
                reasons.Add("Excellent score");
            else if (score >= 100)
                reasons.Add("Good score");
            else if (score < 80)
                reasons.Add("Low score");

            if (activeAssignments < 5)
                reasons.Add("Low workload");
            else if (activeAssignments > 15)
                reasons.Add("High workload");

            if (recentRejections == 0)
                reasons.Add("No recent rejections");
            else if (recentRejections > 3)
                reasons.Add("Multiple recent rejections");

            if (completionRate >= 0.9)
                reasons.Add("High completion rate");
            else if (completionRate < 0.7)
                reasons.Add("Low completion rate");

            return string.Join(", ", reasons);
        }
    }
}

