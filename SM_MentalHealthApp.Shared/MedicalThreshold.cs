using System;
using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    public class MedicalThreshold
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ParameterName { get; set; } = string.Empty; // e.g., "Blood Pressure Systolic", "Hemoglobin", "Triglycerides"

        [StringLength(50)]
        public string? Unit { get; set; } // e.g., "mmHg", "g/dL", "mg/dL"

        [StringLength(50)]
        public string? SeverityLevel { get; set; } // e.g., "Critical", "High", "Low", "Normal"

        public double? MinValue { get; set; } // For range checks (>=)
        public double? MaxValue { get; set; } // For range checks (<=)

        [StringLength(20)]
        public string? ComparisonOperator { get; set; } // e.g., ">=", "<=", ">", "<", "=="

        public double? ThresholdValue { get; set; } // Single threshold value

        [StringLength(100)]
        public string? SecondaryParameterName { get; set; } // For BP: "Diastolic", null for others

        public double? SecondaryThresholdValue { get; set; } // For BP: diastolic threshold, null for others

        [StringLength(20)]
        public string? SecondaryComparisonOperator { get; set; } // For BP: ">=", null for others

        [StringLength(500)]
        public string? Description { get; set; }

        public int Priority { get; set; } = 0; // Higher priority thresholds checked first
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

