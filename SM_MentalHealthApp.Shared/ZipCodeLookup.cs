namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Lookup table for ZIP codes to latitude/longitude mapping
    /// Used for distance calculations in location-based SME matching
    /// </summary>
    public class ZipCodeLookup
    {
        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.MaxLength(10)]
        public string ZipCode { get; set; } = string.Empty;
        
        [System.ComponentModel.DataAnnotations.Required]
        public decimal Latitude { get; set; }
        
        [System.ComponentModel.DataAnnotations.Required]
        public decimal Longitude { get; set; }
        
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? City { get; set; }
        
        [System.ComponentModel.DataAnnotations.MaxLength(2)]
        public string? State { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

