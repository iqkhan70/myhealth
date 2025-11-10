using Microsoft.JSInterop;

namespace SM_MentalHealthApp.Client.Helpers
{
    public static class ConfirmationHelper
    {
        public static async Task<bool> ConfirmAsync(
            this IJSRuntime jsRuntime,
            string message,
            string title = "Confirm Action")
        {
            return await jsRuntime.InvokeAsync<bool>("confirm", $"{title}\n\n{message}");
        }

        public static async Task<bool> ConfirmDestructiveAsync(
            this IJSRuntime jsRuntime,
            string action,
            string details,
            string itemName = "item")
        {
            var message = $"Are you sure you want to {action}?\n\n" +
                         $"{details}\n\n" +
                         $"⚠️ This action cannot be undone!";
            
            return await jsRuntime.InvokeAsync<bool>("confirm", message);
        }
    }
}

