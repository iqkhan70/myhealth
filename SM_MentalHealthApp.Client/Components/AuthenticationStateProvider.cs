using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SM_MentalHealthApp.Client.Services;

namespace SM_MentalHealthApp.Client.Components
{
    public class AuthenticationStateProvider : ComponentBase, IDisposable
    {
        [Inject] protected IAuthService AuthService { get; set; } = null!;
        [Inject] protected ISessionTimeoutService SessionTimeoutService { get; set; } = null!;
        [Inject] protected NavigationManager Navigation { get; set; } = null!;
        [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override async Task OnInitializedAsync()
        {
            // Wait for AuthService to initialize
            await AuthService.InitializeAsync();

            // Check if user is authenticated
            if (!AuthService.IsAuthenticated)
            {
                // No valid token, redirect to login
                Navigation.NavigateTo("/login");
                return;
            }

            // Check if user needs to change password
            var currentUser = AuthService.CurrentUser;
            if (currentUser != null && currentUser.MustChangePassword)
            {
                Navigation.NavigateTo("/change-password");
                return;
            }

            // Start session timeout tracking
            SessionTimeoutService.OnTimeoutWarning += OnTimeoutWarning;
            SessionTimeoutService.OnTimeout += OnTimeout;
            SessionTimeoutService.Start();

            await base.OnInitializedAsync();
        }

        private void OnTimeoutWarning(int remainingMinutes)
        {
            // The warning will be shown by the SessionTimeoutWarning component
            // We'll use a static reference or event to communicate with the warning component
            InvokeAsync(StateHasChanged);
        }

        private void OnTimeout()
        {
            _ = Task.Run(async () =>
            {
                await AuthService.LogoutAsync();
                await InvokeAsync(async () =>
                {
                    // ✅ Force full page reload to clear all component state and cached data
                    try
                    {
                        await JSRuntime.InvokeVoidAsync("eval", "window.location.href = '/login'");
                    }
                    catch
                    {
                        Navigation.NavigateTo("/login", forceLoad: true);
                    }
                });
            });
        }

        public void Dispose()
        {
            SessionTimeoutService.OnTimeoutWarning -= OnTimeoutWarning;
            SessionTimeoutService.OnTimeout -= OnTimeout;
            SessionTimeoutService.Stop();
        }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            if (ChildContent != null)
            {
                builder.AddContent(0, ChildContent);
            }
        }
    }
}
