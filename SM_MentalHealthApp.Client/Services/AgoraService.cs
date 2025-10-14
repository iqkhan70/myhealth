using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;

namespace SM_MentalHealthApp.Client.Services
{
    public interface IAgoraService
    {
        Task<bool> InitializeAsync(string appId);
        Task<bool> JoinChannelAsync(string channelName, string token, uint uid);
        Task<bool> JoinChannelAsync(string channelName, string token, uint uid, bool isVideoCall);
        Task LeaveChannelAsync();
        Task EnableLocalVideoAsync(bool enable);
        Task EnableLocalAudioAsync(bool enable);
        Task SwitchCameraAsync();
        Task MuteLocalAudioAsync(bool mute);
        Task MuteLocalVideoAsync(bool mute);
        Task DestroyAsync();
        bool IsInitialized { get; }
        bool IsInCall { get; }
        string CurrentChannel { get; }
        uint CurrentUid { get; }
        event Action<uint> OnUserJoined;
        event Action<uint> OnUserLeft;
        event Action<string> OnConnectionStateChanged;
        event Action<string> OnError;
        //Task<string> GetAgoraTokenAsync(string channelName);
        Task<string> GetAgoraTokenAsync(string channelName, uint uid = 0);
    }

    public class AgoraService : IAgoraService, IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private bool _isInitialized = false;
        private bool _isInCall = false;
        private string _currentChannel = string.Empty;
        private uint _currentUid = 0;

        public AgoraService(IJSRuntime jsRuntime, HttpClient httpClient, IAuthService authService)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient;
            _authService = authService;
        }

        public bool IsInitialized => _isInitialized;
        public bool IsInCall => _isInCall;
        public string CurrentChannel => _currentChannel;
        public uint CurrentUid => _currentUid;

        public event Action<uint>? OnUserJoined;
        public event Action<uint>? OnUserLeft;
        public event Action<string>? OnConnectionStateChanged;
        public event Action<string>? OnError;

        public async Task<bool> InitializeAsync(string appId)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("agoraInitialize", appId, DotNetObjectReference.Create(this));
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize Agora: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> JoinChannelAsync(string channelName, string token, uint uid)
        {
            return await JoinChannelAsync(channelName, token, uid, true); // Default to video call
        }

        public async Task<bool> JoinChannelAsync(string channelName, string token, uint uid, bool isVideoCall)
        {
            try
            {
                if (!_isInitialized)
                {
                    throw new InvalidOperationException("Agora not initialized");
                }

                Console.WriteLine($"🎯 Joining Agora channel: {channelName} with UID: {uid}, Video: {isVideoCall}");
                await _jsRuntime.InvokeVoidAsync("agoraJoinChannel", channelName, token, uid, isVideoCall);
                _isInCall = true;
                _currentChannel = channelName;
                _currentUid = uid;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to join channel........Iqbal2: {ex.Message}");
                return false;
            }
        }

        public async Task LeaveChannelAsync()
        {
            try
            {
                if (_isInCall)
                {
                    await _jsRuntime.InvokeVoidAsync("agoraLeaveChannel");
                    _isInCall = false;
                    _currentChannel = string.Empty;
                    _currentUid = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to leave channel: {ex.Message}");
            }
        }

        public async Task EnableLocalVideoAsync(bool enable)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("agoraEnableLocalVideo", enable);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to toggle local video: {ex.Message}");
            }
        }

        public async Task EnableLocalAudioAsync(bool enable)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("agoraEnableLocalAudio", enable);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to toggle local audio: {ex.Message}");
            }
        }

        public async Task SwitchCameraAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("agoraSwitchCamera");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to switch camera: {ex.Message}");
            }
        }

        public async Task MuteLocalAudioAsync(bool mute)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("agoraMuteLocalAudio", mute);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to mute/unmute audio: {ex.Message}");
            }
        }

        public async Task MuteLocalVideoAsync(bool mute)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("agoraMuteLocalVideo", mute);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to mute/unmute video: {ex.Message}");
            }
        }

        public async Task DestroyAsync()
        {
            try
            {
                if (_isInCall)
                {
                    await LeaveChannelAsync();
                }
                await _jsRuntime.InvokeVoidAsync("agoraDestroy");
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to destroy Agora: {ex.Message}");
            }
        }

        // public async Task<string> GetAgoraTokenAsync(string channelName)
        // {
        //     try
        //     {
        //         Console.WriteLine($"🎯 Getting Agora token for channel: {channelName}");

        //         // Get token from storage directly
        //         var token = await _jsRuntime.InvokeAsync<string?>("getToken");
        //         if (string.IsNullOrEmpty(token))
        //         {
        //             Console.WriteLine("❌ User not authenticated - no token in storage");
        //             throw new Exception("User not authenticated");
        //         }

        //         Console.WriteLine($"✅ Auth token found: {token.Substring(0, Math.Min(20, token.Length))}...");

        //         var request = new { channelName = channelName, expirationTimeInSeconds = 3600 };

        //         // Set authorization header
        //         _httpClient.DefaultRequestHeaders.Authorization =
        //             new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        //         Console.WriteLine($"🌐 Calling API: {_httpClient.BaseAddress}api/agora/token");
        //         var response = await _httpClient.PostAsJsonAsync("/api/agora/token", request);

        //         Console.WriteLine($"📡 API Response Status: {response.StatusCode}");

        //         if (!response.IsSuccessStatusCode)
        //         {
        //             var errorContent = await response.Content.ReadAsStringAsync();
        //             Console.WriteLine($"❌ API Error: {errorContent}");
        //             throw new Exception($"API call failed: {response.StatusCode} - {errorContent}");
        //         }

        //         var responseContent = await response.Content.ReadAsStringAsync();
        //         Console.WriteLine($"📄 API Response: {responseContent}");

        //         var result = await response.Content.ReadFromJsonAsync<AgoraTokenResponse>();
        //         Console.WriteLine($"✅ Parsed token: {(result?.Token != null ? result.Token.Substring(0, Math.Min(20, result.Token.Length)) : "null")}...");

        //         return result?.Token ?? string.Empty;
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"❌ Failed to get Agora token: {ex.Message}");
        //         Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
        //         return string.Empty;
        //     }
        // }

        public async Task<string> GetAgoraTokenAsync(string channelName, uint uid = 0)
        {
            try
            {
                Console.WriteLine($"🎯 Getting Agora token for channel: {channelName}");

                // ✅ 1. Get user JWT token from browser storage
                var authToken = await _jsRuntime.InvokeAsync<string?>("getToken");
                if (string.IsNullOrEmpty(authToken))
                {
                    Console.WriteLine("❌ User not authenticated - no token in storage");
                    throw new Exception("User not authenticated");
                }

                Console.WriteLine($"✅ Auth token found: {authToken.Substring(0, Math.Min(20, authToken.Length))}...");

                // ✅ 2. Prepare the request URL (GET)
                var url = $"api/realtime/token?channel={Uri.EscapeDataString(channelName)}&uid={uid}";
                Console.WriteLine($"🌐 Calling API: {_httpClient.BaseAddress}{url}");

                // ✅ 3. Attach Authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                // ✅ 4. Make the GET request and parse JSON directly
                var result = await _httpClient.GetFromJsonAsync<AgoraTokenResponse>(url);

                // ✅ 5. Validate and return token
                if (result == null || string.IsNullOrEmpty(result.Token))
                {
                    Console.WriteLine("❌ Invalid or empty Agora token received from API");
                    return string.Empty;
                }

                Console.WriteLine($"✅ Agora token received (cached={result.Cached}): {result.Token.Substring(0, Math.Min(20, result.Token.Length))}...");
                return result.Token;
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"❌ Network error fetching Agora token: {httpEx.Message}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to get Agora token: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return string.Empty;
            }
        }


        // JavaScript interop callbacks
        [JSInvokable]
        public void OnUserJoinedCallback(uint uid)
        {
            OnUserJoined?.Invoke(uid);
        }

        [JSInvokable]
        public void OnUserLeftCallback(uint uid)
        {
            OnUserLeft?.Invoke(uid);
        }

        [JSInvokable]
        public void OnConnectionStateChangedCallback(string state)
        {
            OnConnectionStateChanged?.Invoke(state);
        }

        [JSInvokable]
        public void OnErrorCallback(string error)
        {
            OnError?.Invoke(error);
        }

        public async ValueTask DisposeAsync()
        {
            await DestroyAsync();
        }
    }

    public class AgoraTokenResponse
    {
        [JsonPropertyName("agoraAppId")]
        public string AgoraAppId { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("cached")]
        public bool Cached { get; set; }

        [JsonPropertyName("channelName")]
        public string ChannelName { get; set; } = string.Empty;

        [JsonPropertyName("uid")]
        public uint Uid { get; set; }

        [JsonPropertyName("expirationTime")]
        public DateTime? ExpirationTime { get; set; }
    }
}
