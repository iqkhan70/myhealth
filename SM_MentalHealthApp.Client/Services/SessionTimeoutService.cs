using Microsoft.JSInterop;
using SM_MentalHealthApp.Client.Services;

namespace SM_MentalHealthApp.Client.Services
{
    public interface ISessionTimeoutService
    {
        void Start();
        void Stop();
        void Reset();
        event Action<int> OnTimeoutWarning; // Remaining minutes
        event Action OnTimeout;
    }

    public class SessionTimeoutService : ISessionTimeoutService, IDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly IAuthService _authService;
        private Timer? _timeoutTimer;
        private Timer? _warningTimer;
        private bool _isDisposed = false;
        private bool _isActive = false;

        // Timeout configuration (30 minutes of inactivity)
        private readonly int _timeoutMinutes = 30;
        private readonly int _warningMinutes = 5; // Show warning 5 minutes before timeout

        public event Action<int>? OnTimeoutWarning; // Pass remaining minutes
        public event Action? OnTimeout;

        public SessionTimeoutService(IJSRuntime jsRuntime, IAuthService authService)
        {
            _jsRuntime = jsRuntime;
            _authService = authService;
        }

        public void Start()
        {
            if (_isActive) return;

            _isActive = true;
            Reset();

            // Set up JavaScript event listeners for user activity
            _ = _jsRuntime.InvokeVoidAsync("setupActivityTracking", DotNetObjectReference.Create(this));
        }

        public void Stop()
        {
            _isActive = false;
            _timeoutTimer?.Dispose();
            _warningTimer?.Dispose();
            _timeoutTimer = null;
            _warningTimer = null;

            // Remove JavaScript event listeners
            _ = _jsRuntime.InvokeVoidAsync("removeActivityTracking");
        }

        public void Reset()
        {
            if (!_isActive) return;

            // Dispose existing timers
            _timeoutTimer?.Dispose();
            _warningTimer?.Dispose();

            var timeoutMs = _timeoutMinutes * 60 * 1000;
            var warningMs = (_timeoutMinutes - _warningMinutes) * 60 * 1000;

            // Set up warning timer
            _warningTimer = new Timer(_ =>
            {
                OnTimeoutWarning?.Invoke(_warningMinutes);
            }, null, warningMs, Timeout.Infinite);

            // Set up timeout timer
            _timeoutTimer = new Timer(async _ =>
            {
                OnTimeout?.Invoke();
                await LogoutAsync();
            }, null, timeoutMs, Timeout.Infinite);
        }

        [Microsoft.JSInterop.JSInvokable]
        public void OnUserActivity()
        {
            if (_isActive && _authService.IsAuthenticated)
            {
                Reset();
            }
        }

        private async Task LogoutAsync()
        {
            try
            {
                await _authService.LogoutAsync();
            }
            catch
            {
                // Ignore errors during logout
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            Stop();
            _isDisposed = true;
        }
    }
}

