using Microsoft.AspNetCore.Components.Authorization;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Blazor;

public class AuthState : AuthenticationState
{
    public new User? User { get; }
    public bool IsSignOutForced { get; }

    public AuthState() : this(null) { }
    public AuthState(User? user, bool isSignOutForced = false)
        : base(user.OrGuest().ToClaimsPrincipal())
    {
        User = user;
        IsSignOutForced = isSignOutForced;
    }
}
