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
        
        [Required]
        public DateTime DateOfBirth { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Gender { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string MobilePhone { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
        
        public UserRequestStatus Status { get; set; } = UserRequestStatus.Pending;
        
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        
        public int? ReviewedByUserId { get; set; }
        
        public DateTime? ReviewedAt { get; set; }
        
        [MaxLength(2000)]
        public string? Notes { get; set; }
        
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

