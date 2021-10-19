using Microsoft.AspNetCore.Components.Authorization;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Blazor;

public class AuthState : AuthenticationState
{
    public new User User { get; }
    public bool IsSignOutForced { get; }

    public AuthState(User user, bool isSignOutForced = false)
        : base(user.ClaimsPrincipal)
    {
        User = user;
        IsSignOutForced = isSignOutForced;
    }
}
