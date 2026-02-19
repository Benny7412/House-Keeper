using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace HouseKeeper.Components.Services;

public sealed class CurrentUserAccessor
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public CurrentUserAccessor(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<ClaimsPrincipal> GetRequiredPrincipalAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var principal = authState.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("You must be signed in to perform this action.");
        }

        return principal;
    }

    public async Task<string> GetRequiredUserIdAsync()
    {
        var principal = await GetRequiredPrincipalAsync();
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Authenticated user id was not found.");
        }

        return userId;
    }
}
