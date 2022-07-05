using Microsoft.AspNetCore.Components.Authorization;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Blazor;

public class AuthState : AuthenticationState
{
    public new User? User { get; }
    public Session Session { get; }
    public bool IsSignOutForced { get; }

    public AuthState(User? user, Session session, bool isSignOutForced = false)
        : base(user.OrGuest(session).ClaimsPrincipal)
    {
        User = user;
        Session = session;
        IsSignOutForced = isSignOutForced;
    }
}
