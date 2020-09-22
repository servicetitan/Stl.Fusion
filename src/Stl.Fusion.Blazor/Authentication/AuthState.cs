using Microsoft.AspNetCore.Components.Authorization;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Blazor.Authentication
{
    public class AuthState : AuthenticationState
    {
        public new User User { get; }

        public AuthState(User user)
            : base(user.ClaimsPrincipal)
            => User = user;
    }
}
