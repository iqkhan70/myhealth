using System.Net.Http.Headers;
using SM_MentalHealthApp.Client.Services;

namespace SM_MentalHealthApp.Client.Services;

/// <summary>
/// Base class for all client-side services to provide common functionality
/// </summary>
public abstract class BaseService
{
    protected readonly HttpClient _http;
    protected readonly IAuthService _authService;

    protected BaseService(HttpClient http, IAuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    /// <summary>
    /// Adds the authorization header to the HTTP client if a token is available
    /// </summary>
    protected void AddAuthorizationHeader()
    {
        var token = _authService.Token;
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}

