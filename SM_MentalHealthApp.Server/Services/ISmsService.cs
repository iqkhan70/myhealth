using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface ISmsService
    {
        /// <summary>
        /// Send SMS message to a phone number
        /// </summary>
        /// <param name="phoneNumber">Recipient phone number (e.g., +1234567890)</param>
        /// <param name="message">SMS message content</param>
        /// <returns>True if SMS was sent successfully, false otherwise</returns>
        Task<bool> SendSmsAsync(string phoneNumber, string message);

        /// <summary>
        /// Send emergency alert SMS to a doctor
        /// </summary>
        /// <param name="doctorPhoneNumber">Doctor's phone number</param>
        /// <param name="alert">Emergency alert details</param>
        /// <returns>True if SMS was sent successfully, false otherwise</returns>
        Task<bool> SendEmergencyAlertAsync(string doctorPhoneNumber, EmergencyAlert alert);

        /// <summary>
        /// Send bulk SMS to multiple recipients
        /// </summary>
        /// <param name="phoneNumbers">List of phone numbers</param>
        /// <param name="message">SMS message content</param>
        /// <returns>Number of SMS messages sent successfully</returns>
        Task<int> SendBulkSmsAsync(List<string> phoneNumbers, string message);

        /// <summary>
        /// Check if SMS service is configured and ready
        /// </summary>
        /// <returns>True if service is ready, false otherwise</returns>
        bool IsServiceReady();
    }
}
