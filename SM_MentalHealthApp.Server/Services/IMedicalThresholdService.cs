using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IMedicalThresholdService
    {
        Task<List<MedicalThreshold>> GetActiveThresholdsAsync(string parameterName);
        Task<bool> IsValueCriticalAsync(string parameterName, double value, double? secondaryValue = null);
        Task<MedicalThreshold?> GetMatchingThresholdAsync(string parameterName, double value, double? secondaryValue = null);
        Task<string?> GetSeverityLevelAsync(string parameterName, double value, double? secondaryValue = null);
    }
}

