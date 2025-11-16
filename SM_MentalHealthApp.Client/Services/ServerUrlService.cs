using Microsoft.JSInterop;

namespace SM_MentalHealthApp.Client.Services
{
    /// <summary>
    /// Service to configure the server URL when using ngrok.
    /// Reads from URL query parameter or localStorage and updates HttpClient.
    /// </summary>
    public class ServerUrlService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _httpClient;

        public ServerUrlService(IJSRuntime jsRuntime, HttpClient httpClient)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Initializes the server URL from query parameter or localStorage.
        /// Call this during app initialization if accessing via ngrok or from another machine.
        /// </summary>
        public async Task InitializeServerUrlAsync()
        {
            try
            {
                var currentUrl = await _jsRuntime.InvokeAsync<string>("eval", "window.location.href");
                var currentHost = await _jsRuntime.InvokeAsync<string>("eval", "window.location.hostname");
                
                Console.WriteLine($"🔍 ServerUrlService: Current URL: {currentUrl}");
                Console.WriteLine($"🔍 ServerUrlService: Current hostname: {currentHost}");
                
                // Check if we're accessing via ngrok OR from another machine (not localhost)
                var isNgrok = currentUrl.Contains("ngrok.io") || currentUrl.Contains("ngrok-free.app");
                var isLocalhost = currentHost == "localhost" || currentHost == "127.0.0.1" || currentHost == "::1";
                var isRemoteAccess = !isLocalhost && !isNgrok;
                
                Console.WriteLine($"🔍 ServerUrlService: isNgrok={isNgrok}, isLocalhost={isLocalhost}, isRemoteAccess={isRemoteAccess}");

                // Try to get server URL from query parameter first (highest priority)
                var serverUrl = await _jsRuntime.InvokeAsync<string>("eval", 
                    "new URLSearchParams(window.location.search).get('server')");

                // If not in query, try localStorage
                if (string.IsNullOrWhiteSpace(serverUrl))
                {
                    serverUrl = await _jsRuntime.InvokeAsync<string>("eval", 
                        "localStorage.getItem('server_ngrok_url')");
                }

                if (!string.IsNullOrWhiteSpace(serverUrl))
                {
                    serverUrl = serverUrl.Trim();
                    if (!serverUrl.EndsWith("/"))
                        serverUrl += "/";

                    // Update HttpClient BaseAddress
                    _httpClient.BaseAddress = new Uri(serverUrl);
                    
                    // Save to localStorage for next time
                    await _jsRuntime.InvokeVoidAsync("eval", 
                        $"localStorage.setItem('server_ngrok_url', '{serverUrl}')");

                    Console.WriteLine($"✅ Server URL configured from query/localStorage: {serverUrl}");
                }
                else if (isNgrok)
                {
                    // Using ngrok but no server URL provided
                    Console.WriteLine($"❌ No server URL found in query parameter or localStorage.");
                    Console.WriteLine($"❌ When accessing via ngrok, you must provide the server URL.");
                    Console.WriteLine($"❌ Add ?server=https://your-server-ngrok-url.ngrok.io to the URL");
                    Console.WriteLine($"❌ Example: {currentUrl}?server=https://abc123.ngrok.io");
                }
                else if (isRemoteAccess)
                {
                    // Accessing from another machine but not via ngrok
                    // The default HttpClient configuration should handle this, but log a warning
                    Console.WriteLine($"⚠️ Accessing from remote machine ({currentHost}) without ngrok.");
                    Console.WriteLine($"⚠️ Make sure the server is accessible at the configured address.");
                    Console.WriteLine($"⚠️ For better reliability, consider using ngrok with ?server= parameter.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error initializing server URL: {ex.Message}");
                Console.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
            }
        }
    }
}

