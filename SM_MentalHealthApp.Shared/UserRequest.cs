using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Represents a user registration request from a guest user
    /// </summary>
    public class UserRequest
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        // Encrypted DateOfBirth stored in database (not sent to client for security)
        [System.Text.Json.Serialization.JsonIgnore]
        public string DateOfBirthEncrypted { get; set; } = string.Empty;
        
        // Computed property for decrypted DateOfBirth (not stored in DB, sent to client)
        // This will be populated by the service layer after decryption
        private DateTime _decryptedDateOfBirth = DateTime.MinValue;
        public DateTime DateOfBirth 
        { 
            get 
            {
                // This will be set by the service layer after decryption
                return _decryptedDateOfBirth;
            }
            set 
            {
                _decryptedDateOfBirth = value;
            }
        }
        
        [Required]
        [MaxLength(20)]
        public string Gender { get; set; } = string.Empty;
        
        // Encrypted MobilePhone stored in database (not sent to client for security)
        [System.Text.Json.Serialization.JsonIgnore]
        public string MobilePhoneEncrypted { get; set; } = string.Empty;
        
        // Computed property for decrypted MobilePhone (not stored in DB, sent to client)
        // This will be populated by the service layer after decryption
        private string _decryptedMobilePhone = string.Empty;
        public string MobilePhone 
        { 
            get 
            {
                // This will be set by the service layer after decryption
                return _decryptedMobilePhone;
            }
            set 
            {
                _decryptedMobilePhone = value;
            }
        }
        
        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
        
        public UserRequestStatus Status { get; set; } = UserRequestStatus.Pending;
        
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        
        public int? ReviewedByUserId { get; set; }
        
        public DateTime? ReviewedAt { get; set; }
        
        [MaxLength(2000)]
        public string? Notes { get; set; }
        
        // Accident-related fields for coordinators to capture
        public int? Age { get; set; }
        
        [MaxLength(100)]
        public string? Race { get; set; }
        
        [MaxLength(500)]
        public string? AccidentAddress { get; set; }
        
        public DateTime? AccidentDate { get; set; }
        
        [MaxLength(1000)]
        public string? VehicleDetails { get; set; }
        
        public DateTime? DateReported { get; set; }
        
        [MaxLength(100)]
        public string? PoliceCaseNumber { get; set; }
        
        [MaxLength(2000)]
        public string? AccidentDetails { get; set; }
        
        [MaxLength(200)]
        public string? RoadConditions { get; set; }
        
        [MaxLength(1000)]
        public string? DoctorsInformation { get; set; }
        
        [MaxLength(1000)]
        public string? LawyersInformation { get; set; }
        
        [MaxLength(2000)]
        public string? AdditionalNotes { get; set; }
        
        // New Accident fields from Script121525.sql
        // 1) Detailed Injury Assessment
        public int? SymptomOngoingStatusId { get; set; }
        public bool? SymptomsHeadaches { get; set; }
        public bool? SymptomsDizziness { get; set; }
        public bool? SymptomsNeckPain { get; set; }
        public bool? SymptomsBackPain { get; set; }
        public bool? SymptomsJointPain { get; set; }
        public bool? SymptomsNumbnessTingling { get; set; }

        // 2) Medical Treatment Details
        public bool? WentToEmergencyRoom { get; set; }
        [MaxLength(255)]
        public string? ERHospitalName { get; set; }
        public DateTime? ERVisitDate { get; set; }
        public bool? TreatingInjurySpecialist { get; set; }
        [MaxLength(1000)]
        public string? InjurySpecialistDetails { get; set; }

        // 3) Insurance & Claims Handling
        public bool? InsuranceAdjusterContacted { get; set; }
        public bool? ProvidedRecordedStatement { get; set; }
        public bool? ReceivedSettlementOffer { get; set; }
        public decimal? SettlementOfferAmount { get; set; }
        [MaxLength(255)]
        public string? ClaimInsuranceCompany { get; set; }

        // 4) Legal Representation
        public bool? SignedDocumentsRelatedToAccident { get; set; }
        [MaxLength(1000)]
        public string? SignedDocumentsNotes { get; set; }
        [MaxLength(255)]
        public string? AttorneyName { get; set; }
        [MaxLength(255)]
        public string? AttorneyFirm { get; set; }

        // 5) Vehicle & Property Damage
        [MaxLength(500)]
        public string? VehicleCurrentLocation { get; set; }
        public bool? InsuranceEstimateCompleted { get; set; }
        public decimal? EstimatedRepairAmount { get; set; }
        public bool? VehicleTotalLoss { get; set; }

        // 6) Work & Life Impact
        public bool? MissedWork { get; set; }
        public int? MissedWorkDays { get; set; }
        public bool? WorkingWithRestrictions { get; set; }
        [MaxLength(1000)]
        public string? WorkRestrictionDetails { get; set; }
        public bool? DailyActivitiesAffected { get; set; }
        [MaxLength(1000)]
        public string? DailyActivitiesNotes { get; set; }
        
        // Lead Intake fields
        [MaxLength(2)]
        public string? ResidenceStateCode { get; set; }
        
        [MaxLength(2)]
        public string? AccidentStateCode { get; set; }
        
        public int? AccidentParticipantRoleId { get; set; }
        
        public int? VehicleDispositionId { get; set; }
        
        public int? TransportToCareMethodId { get; set; }
        
        public int? MedicalAttentionTypeId { get; set; }
        
        public bool? PoliceInvolvement { get; set; }
        
        public bool? LostConsciousness { get; set; }
        
        public bool? NeuroSymptoms { get; set; }
        
        public bool? MusculoskeletalSymptoms { get; set; }
        
        public bool? PsychologicalSymptoms { get; set; }
        
        [MaxLength(2000)]
        public string? SymptomsNotes { get; set; }
        
        public bool? InsuranceContacted { get; set; }
        
        public bool? RepresentedByAttorney { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public User? ReviewedByUser { get; set; }
        
        public string FullName => $"{FirstName} {LastName}";
    }
    
    public enum UserRequestStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
    
    // Request models for API
    public class CreateUserRequestRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public DateTime DateOfBirth { get; set; }
        
        [Required]
        public string Gender { get; set; } = string.Empty;
        
        [Required]
        public string MobilePhone { get; set; } = string.Empty;
        
        [Required]
        [MinLength(10)]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
    }
    
    public class ApproveUserRequestRequest
    {
        [Required]
        [MinLength(10)]
        public string Notes { get; set; } = string.Empty;
    }
    
    public class RejectUserRequestRequest
    {
        [Required]
        [MinLength(10)]
        public string Notes { get; set; } = string.Empty;
    }
    
    public class MarkPendingUserRequestRequest
    {
        [Required]
        [MinLength(10)]
        public string Notes { get; set; } = string.Empty;
    }
}

