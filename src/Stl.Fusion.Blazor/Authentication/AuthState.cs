using Microsoft.AspNetCore.Components.Authorization;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Blazor.Authentication
{
    public class AuthState : AuthenticationState
    {
        public new AuthUser User { get; }

        public AuthState(AuthUser authUser)
            : base(authUser.ClaimsPrincipal)
            => User = authUser;
    }
}
