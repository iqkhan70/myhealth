using Microsoft.AspNetCore.Components;
using SM_MentalHealthApp.Client.Services;

namespace SM_MentalHealthApp.Client.Components
{
    public class AuthenticationStateProvider : ComponentBase
    {
        [Inject] protected IAuthService AuthService { get; set; } = null!;
        [Inject] protected NavigationManager Navigation { get; set; } = null!;
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

            await base.OnInitializedAsync();
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
