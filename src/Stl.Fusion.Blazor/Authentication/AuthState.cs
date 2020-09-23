using Microsoft.AspNetCore.Components.Authorization;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Blazor
{
    public class AuthState : AuthenticationState
    {
        public new User User { get; }
        public bool IsLogoutForced { get; }

        public AuthState(User user, bool isLogoutForced = false)
            : base(user.ClaimsPrincipal)
        {
            User = user;
            IsLogoutForced = isLogoutForced;
        }
    }
}
