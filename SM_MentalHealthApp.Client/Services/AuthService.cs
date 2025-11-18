using System.Net.Http.Json;
using Microsoft.JSInterop;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RegisterAsync(RegisterRequest request);
        Task<bool> ValidateTokenAsync();
        Task<AuthUser?> GetCurrentUserAsync();
        Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request);
        Task LogoutAsync();
        Task InitializeAsync();
        bool IsAuthenticated { get; }
        AuthUser? CurrentUser { get; }
        string? Token { get; }
        bool IsInitialized { get; }
        event Action? OnAuthenticationStateChanged;
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private AuthUser? _currentUser;
        private string? _token;
        private bool _isInitialized = false;
        private bool _isInitializing = false;

        public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized || _isInitializing) return;
            
            _isInitializing = true;
            try
            {
                // ✅ Get token from sessionStorage (persists across page refresh)
                _token = await GetTokenFromStorageAsync();
                if (!string.IsNullOrEmpty(_token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                    
                    // ✅ Validate token and get user (validates against Redis session)
                    await GetCurrentUserAsync();
                }
            }
            finally
            {
                _isInitialized = true;
                _isInitializing = false;
            }
        }

        public bool IsAuthenticated => _isInitialized && !string.IsNullOrEmpty(_token) && _currentUser != null;
        public AuthUser? CurrentUser => _currentUser;
        public string? Token => _token;
        public bool IsInitialized => _isInitialized;

        public event Action? OnAuthenticationStateChanged;

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var apiUrl = $"{_httpClient.BaseAddress}api/auth/login";
                Console.WriteLine($"🔐 Login: Calling API at {apiUrl}");
                Console.WriteLine($"🔐 Login: BaseAddress is {_httpClient.BaseAddress}");
                
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
                
                Console.WriteLine($"🔐 Login: Response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Login: Error response: {errorContent}");
                    return new LoginResponse { Success = false, Message = $"Login failed: {response.StatusCode} - {errorContent}" };
                }
                
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (result?.Success == true)
                {
                    Console.WriteLine($"✅ Login: Success! User: {result.User?.Email}");
                    _token = result.Token;
                    _currentUser = new AuthUser
                    {
                        Id = result.User!.Id,
                        Email = result.User.Email,
                        FirstName = result.User.FirstName,
                        LastName = result.User.LastName,
                        RoleId = result.User.RoleId,
                        RoleName = result.User.Role?.Name ?? "Patient",
                        IsFirstLogin = result.User.IsFirstLogin,
                        MustChangePassword = result.User.MustChangePassword
                    };

                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                    await SaveTokenToStorageAsync(_token);
                    OnAuthenticationStateChanged?.Invoke();
                }
                else
                {
                    Console.WriteLine($"❌ Login: Failed - {result?.Message}");
                }

                return result ?? new LoginResponse { Success = false, Message = "Login failed" };
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"❌ Login: HTTP Exception - {httpEx.Message}");
                Console.WriteLine($"❌ Login: Inner exception - {httpEx.InnerException?.Message}");
                Console.WriteLine($"❌ Login: Server URL: {_httpClient.BaseAddress}");
                Console.WriteLine($"❌ Login: Troubleshooting:");
                Console.WriteLine($"   1. Is server running? Check: lsof -i :5262");
                Console.WriteLine($"   2. SSL Certificate issue? Try accessing {_httpClient.BaseAddress}api/health directly in browser");
                Console.WriteLine($"   3. Accept the certificate warning in browser first");
                Console.WriteLine($"   4. Is firewall blocking? Check macOS firewall settings");
                Console.WriteLine($"   5. Can you reach server? Try: curl -k {_httpClient.BaseAddress}api/health");
                Console.WriteLine($"   6. Are both machines on same network?");
                Console.WriteLine($"   7. For self-signed certs: See FIX_SSL_CERTIFICATE_ERROR.md");
                return new LoginResponse { Success = false, Message = $"Cannot connect to server at {_httpClient.BaseAddress}. This is likely an SSL certificate issue. Try: 1) Access {_httpClient.BaseAddress}api/health in browser and accept certificate, 2) Check FIX_SSL_CERTIFICATE_ERROR.md" };
            }
            catch (TaskCanceledException timeoutEx)
            {
                Console.WriteLine($"❌ Login: Timeout - {timeoutEx.Message}");
                return new LoginResponse { Success = false, Message = "Login request timed out. Please check your network connection." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Login: Exception - {ex.Message}");
                Console.WriteLine($"❌ Login: Exception type - {ex.GetType().Name}");
                Console.WriteLine($"❌ Login: Stack trace - {ex.StackTrace}");
                // ✅ Don't let exceptions crash the runtime - return error response instead
                return new LoginResponse { Success = false, Message = $"Login failed: {ex.Message}" };
            }
        }

        public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (result?.Success == true)
                {
                    _token = result.Token;
                    _currentUser = new AuthUser
                    {
                        Id = result.User!.Id,
                        Email = result.User.Email,
                        FirstName = result.User.FirstName,
                        LastName = result.User.LastName,
                        RoleId = result.User.RoleId,
                        RoleName = result.User.Role?.Name ?? "Patient",
                        IsFirstLogin = result.User.IsFirstLogin,
                        MustChangePassword = result.User.MustChangePassword
                    };

                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                    await SaveTokenToStorageAsync(_token);
                    OnAuthenticationStateChanged?.Invoke();
                }

                return result ?? new LoginResponse { Success = false, Message = "Registration failed" };
            }
            catch (Exception ex)
            {
                return new LoginResponse { Success = false, Message = $"Registration failed: {ex.Message}" };
            }
        }

        public async Task<bool> ValidateTokenAsync()
        {
            // ✅ First try to get token from sessionStorage (in case of page refresh)
            if (string.IsNullOrEmpty(_token))
            {
                _token = await GetTokenFromStorageAsync();
            }

            if (string.IsNullOrEmpty(_token))
            {
                return false;
            }

            try
            {
                // ✅ Validate token against server (checks both JWT validity and Redis session)
                var response = await _httpClient.PostAsJsonAsync("api/auth/validate", _token);
                if (response.IsSuccessStatusCode)
                {
                    var isValid = await response.Content.ReadFromJsonAsync<bool>();
                    if (isValid)
                    {
                        // ✅ If token is valid, ensure it's set in Authorization header
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                    }
                    else
                    {
                        // ✅ Token invalid - clear it
                        _token = null;
                        await RemoveTokenFromStorageAsync();
                    }
                    return isValid;
                }
            }
            catch
            {
                // If validation fails, assume token is invalid
            }

            // ✅ Clear invalid token
            _token = null;
            await RemoveTokenFromStorageAsync();
            return false;
        }

        public async Task<AuthUser?> GetCurrentUserAsync()
        {
            // ✅ First try to get token from sessionStorage (in case of page refresh)
            if (string.IsNullOrEmpty(_token))
            {
                _token = await GetTokenFromStorageAsync();
                if (!string.IsNullOrEmpty(_token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                }
            }

            if (string.IsNullOrEmpty(_token))
            {
                return null;
            }

            try
            {
                var response = await _httpClient.GetAsync("api/auth/user");
                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<AuthUser>();
                    _currentUser = user;
                    return user;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // ✅ Token invalid - clear it
                    _token = null;
                    _currentUser = null;
                    await RemoveTokenFromStorageAsync();
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }
            }
            catch
            {
                // If request fails, return null
            }

            return null;
        }

        public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                // Authorization header should already be set during initialization
                var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request);
                var result = await response.Content.ReadFromJsonAsync<ChangePasswordResponse>();

                if (result?.Success == true)
                {
                    // Refresh user data to clear MustChangePassword flag
                    await GetCurrentUserAsync();
                }

                return result ?? new ChangePasswordResponse { Success = false, Message = "Change password failed" };
            }
            catch (Exception ex)
            {
                return new ChangePasswordResponse { Success = false, Message = $"Change password failed: {ex.Message}" };
            }
        }

        public async Task LogoutAsync()
        {
            // ✅ Call server to invalidate session in Redis
            if (!string.IsNullOrEmpty(_token))
            {
                try
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                    await _httpClient.PostAsync("api/auth/logout", null);
                }
                catch
                {
                    // Continue with logout even if server call fails
                }
            }

            // ✅ Clear token from sessionStorage and memory
            _token = null;
            _currentUser = null;
            await RemoveTokenFromStorageAsync();
            _httpClient.DefaultRequestHeaders.Authorization = null;
            OnAuthenticationStateChanged?.Invoke();
        }

        // ✅ Store token in sessionStorage (persists on refresh, cleared on tab close)
        // Session is validated and managed server-side via Redis
        // Token is also kept in memory for immediate access
        private async Task SaveTokenToStorageAsync(string token)
        {
            await _jsRuntime.InvokeVoidAsync("saveToken", token);
        }

        private async Task<string?> GetTokenFromStorageAsync()
        {
            // Get token from sessionStorage (persists across page refresh)
            var token = await _jsRuntime.InvokeAsync<string?>("getToken");
            if (!string.IsNullOrEmpty(token))
            {
                _token = token; // Also store in memory for immediate access
            }
            return token;
        }

        private async Task RemoveTokenFromStorageAsync()
        {
            await _jsRuntime.InvokeVoidAsync("removeToken");
        }
    }
}
