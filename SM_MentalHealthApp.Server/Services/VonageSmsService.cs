using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SM_MentalHealthApp.Shared;
using System.Text;
using System.Text.Json;

namespace SM_MentalHealthApp.Server.Services
{
    public class VonageSmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VonageSmsService> _logger;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _fromNumber;
        private readonly bool _isEnabled;

        public VonageSmsService(HttpClient httpClient, IConfiguration configuration, ILogger<VonageSmsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Get Vonage configuration from appsettings.json
            _apiKey = _configuration["Vonage:ApiKey"] ?? "";
            _apiSecret = _configuration["Vonage:ApiSecret"] ?? "";
            _fromNumber = _configuration["Vonage:FromNumber"] ?? "";
            _isEnabled = _configuration.GetValue<bool>("Vonage:Enabled", false);

            _logger.LogInformation("Vonage SMS Service initialized. Enabled: {IsEnabled}, HasApiKey: {HasApiKey}",
                _isEnabled, !string.IsNullOrEmpty(_apiKey));
        }

        public bool IsServiceReady()
        {
            return _isEnabled &&
                   !string.IsNullOrEmpty(_apiKey) &&
                   !string.IsNullOrEmpty(_apiSecret) &&
                   !string.IsNullOrEmpty(_fromNumber);
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            if (!IsServiceReady())
            {
                _logger.LogWarning("SMS service not ready. Skipping SMS to {PhoneNumber}", phoneNumber);
                return false;
            }

            try
            {
                _logger.LogInformation("Sending SMS to {PhoneNumber}: {Message}", phoneNumber, message);

                // TODO: Replace with actual Vonage API call
                // For now, this is a placeholder implementation
                var success = await SendVonageSmsAsync(phoneNumber, message);

                if (success)
                {
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}", phoneNumber);
                }
                else
                {
                    _logger.LogError("Failed to send SMS to {PhoneNumber}", phoneNumber);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendEmergencyAlertAsync(string doctorPhoneNumber, EmergencyAlert alert)
        {
            var message = FormatEmergencyMessage(alert);
            return await SendSmsAsync(doctorPhoneNumber, message);
        }

        public async Task<int> SendBulkSmsAsync(List<string> phoneNumbers, string message)
        {
            if (!IsServiceReady())
            {
                _logger.LogWarning("SMS service not ready. Skipping bulk SMS to {Count} recipients", phoneNumbers.Count);
                return 0;
            }

            var successCount = 0;
            var tasks = phoneNumbers.Select(async phoneNumber =>
            {
                var success = await SendSmsAsync(phoneNumber, message);
                if (success) Interlocked.Increment(ref successCount);
            });

            await Task.WhenAll(tasks);
            return successCount;
        }

        private async Task<bool> SendVonageSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // TODO: Replace this with actual Vonage API implementation
                // This is a placeholder that simulates the API call

                _logger.LogInformation("Sending SMS via Vonage API - From: {FromNumber}, To: {PhoneNumber}", _fromNumber, phoneNumber);
                _logger.LogInformation("Message: {Message}", message);

                // ACTUAL VONAGE API IMPLEMENTATION:
                var requestBody = new
                {
                    from = _fromNumber,
                    to = phoneNumber,
                    text = message,
                    type = "text"
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Making Vonage API call to: https://rest.nexmo.com/sms/json");
                _logger.LogInformation("Request body: {RequestBody}", json);

                var response = await _httpClient.PostAsync(
                    $"https://rest.nexmo.com/sms/json?api_key={_apiKey}&api_secret={_apiSecret}",
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Vonage API Response: Status={StatusCode}, Content={Content}",
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // Check if message was accepted
                    if (result.TryGetProperty("messages", out var messages) && messages.GetArrayLength() > 0)
                    {
                        var firstMessage = messages[0];
                        if (firstMessage.TryGetProperty("status", out var status))
                        {
                            var statusValue = status.GetString();
                            _logger.LogInformation("Vonage message status: {Status}", statusValue);
                            return statusValue == "0"; // 0 means success in Vonage API
                        }
                    }
                }
                else
                {
                    _logger.LogError("Vonage API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Vonage API call to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        private string FormatEmergencyMessage(EmergencyAlert alert)
        {
            var sb = new StringBuilder();
            sb.AppendLine("🚨 EMERGENCY ALERT 🚨");
            sb.AppendLine();
            sb.AppendLine($"Patient: {alert.PatientName}");
            sb.AppendLine($"Type: {alert.EmergencyType}");
            sb.AppendLine($"Severity: {alert.Severity}");
            sb.AppendLine($"Time: {alert.Timestamp:MM/dd/yyyy HH:mm}");

            if (!string.IsNullOrEmpty(alert.Message))
            {
                sb.AppendLine($"Details: {alert.Message}");
            }

            if (alert.VitalSigns != null)
            {
                sb.AppendLine();
                sb.AppendLine("Vital Signs:");
                if (alert.VitalSigns.HeartRate.HasValue)
                    sb.AppendLine($"  Heart Rate: {alert.VitalSigns.HeartRate} bpm");
                if (!string.IsNullOrEmpty(alert.VitalSigns.BloodPressure))
                    sb.AppendLine($"  Blood Pressure: {alert.VitalSigns.BloodPressure}");
                if (alert.VitalSigns.Temperature.HasValue)
                    sb.AppendLine($"  Temperature: {alert.VitalSigns.Temperature:F1}°F");
                if (alert.VitalSigns.OxygenSaturation.HasValue)
                    sb.AppendLine($"  Oxygen: {alert.VitalSigns.OxygenSaturation}%");
            }

            sb.AppendLine();
            sb.AppendLine("Please check the Emergency Dashboard immediately!");
            sb.AppendLine("Reply STOP to opt out of emergency alerts.");

            return sb.ToString();
        }
    }
}
