namespace SM_MentalHealthApp.Shared
{
    public class ClinicalRecommendation
    {
        public string Diagnosis { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Severity { get; set; } = string.Empty;
        public List<string> ImmediateActions { get; set; } = new();
        public List<FollowUpStep> FollowUpSteps { get; set; } = new();
        public ClinicalProtocol ClinicalProtocol { get; set; } = new();
        public List<InsuranceRequirement> InsuranceRequirements { get; set; } = new();
        public List<string> PatientSpecificNotes { get; set; } = new();
        public List<string> RiskFactors { get; set; } = new();
        public List<string> Contraindications { get; set; } = new();
        public List<string> AlternativeTreatments { get; set; } = new();
    }

    public class FollowUpStep
    {
        public string Step { get; set; } = string.Empty;
        public string Timeline { get; set; } = string.Empty;
        public string ResponsibleParty { get; set; } = string.Empty;
        public string DocumentationRequired { get; set; } = string.Empty;
        public string InsuranceConsiderations { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
    }

    public class InsuranceRequirement
    {
        public string Requirement { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class ClinicalProtocol
    {
        public List<string> DiagnosticCriteria { get; set; } = new();
        public List<string> TreatmentGuidelines { get; set; } = new();
        public List<string> MonitoringRequirements { get; set; } = new();
        public List<string> SafetyConsiderations { get; set; } = new();
        public List<string> ReferralCriteria { get; set; } = new();
        public List<string> EmergencyProtocols { get; set; } = new();
    }

    public class ClinicalRecommendationRequest
    {
        public string Diagnosis { get; set; } = string.Empty;
        public int PatientId { get; set; }
    }

    public class SymptomAnalysisRequest
    {
        public string Symptoms { get; set; } = string.Empty;
        public int PatientId { get; set; }
    }

    public class DiagnosisSuggestion
    {
        public string Diagnosis { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    // Response DTOs for JSON deserialization
    public class AIRecommendationResponse
    {
        public string Severity { get; set; } = "Moderate";
        public List<string> ImmediateActions { get; set; } = new();
        public List<string> PatientSpecificNotes { get; set; } = new();
        public List<string> RiskFactors { get; set; } = new();
        public List<string> Contraindications { get; set; } = new();
        public List<string> AlternativeTreatments { get; set; } = new();
    }

    public class FollowUpResponse
    {
        public List<FollowUpStep> FollowUpSteps { get; set; } = new();
    }

    public class InsuranceResponse
    {
        public List<InsuranceRequirement> InsuranceRequirements { get; set; } = new();
    }

    public class ProtocolResponse
    {
        public ClinicalProtocol Protocol { get; set; } = new();
    }
}
