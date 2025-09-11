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
                _token = await GetTokenFromStorageAsync();
                if (!string.IsNullOrEmpty(_token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
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
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (result?.Success == true)
                {
                    _token = result.Token;
                    _currentUser = new AuthUser
                    {
                        Id = result.Patient!.Id,
                        Email = result.Patient.Email,
                        FirstName = result.Patient.FirstName,
                        LastName = result.Patient.LastName,
                        RoleId = result.Patient.RoleId,
                        RoleName = result.Patient.Role?.Name ?? "Patient",
                        IsFirstLogin = result.Patient.IsFirstLogin,
                        MustChangePassword = result.Patient.MustChangePassword
                    };

                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                    await SaveTokenToStorageAsync(_token);
                    OnAuthenticationStateChanged?.Invoke();
                }

                return result ?? new LoginResponse { Success = false, Message = "Login failed" };
            }
            catch (Exception ex)
            {
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
                        Id = result.Patient!.Id,
                        Email = result.Patient.Email,
                        FirstName = result.Patient.FirstName,
                        LastName = result.Patient.LastName,
                        RoleId = result.Patient.RoleId,
                        RoleName = result.Patient.Role?.Name ?? "Patient",
                        IsFirstLogin = result.Patient.IsFirstLogin,
                        MustChangePassword = result.Patient.MustChangePassword
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
            if (string.IsNullOrEmpty(_token))
                return false;

            try
            {
                // Authorization header should already be set during initialization
                var response = await _httpClient.PostAsJsonAsync("api/auth/validate", _token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<AuthUser?> GetCurrentUserAsync()
        {
            if (string.IsNullOrEmpty(_token))
                return null;

            try
            {
                // Authorization header should already be set during initialization
                var response = await _httpClient.GetAsync("api/auth/user");
                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<AuthUser>();
                    _currentUser = user;
                    return user;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token is invalid, clear it
                    _token = null;
                    _currentUser = null;
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                    await RemoveTokenFromStorageAsync();
                }
            }
            catch
            {
                // Token might be invalid, clear it
                _token = null;
                _currentUser = null;
                _httpClient.DefaultRequestHeaders.Authorization = null;
                await RemoveTokenFromStorageAsync();
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
            _token = null;
            _currentUser = null;
            await RemoveTokenFromStorageAsync();
            _httpClient.DefaultRequestHeaders.Authorization = null;
            OnAuthenticationStateChanged?.Invoke();
        }

        private async Task SaveTokenToStorageAsync(string token)
        {
            await _jsRuntime.InvokeVoidAsync("saveToken", token);
        }

        private async Task<string?> GetTokenFromStorageAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>("getToken");
        }

        private async Task RemoveTokenFromStorageAsync()
        {
            await _jsRuntime.InvokeVoidAsync("removeToken");
        }
    }
}
