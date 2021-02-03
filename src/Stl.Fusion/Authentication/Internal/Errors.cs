using System;
using System.Security;

namespace Stl.Fusion.Authentication.Internal
{
    public static class Errors
    {
        public static Exception InvalidSessionId(string parameterName)
            => new ArgumentOutOfRangeException(parameterName, "Provided Session.Id is invalid.");
        public static Exception NoSessionProvided(string? parameterName = null)
            => new InvalidOperationException("No Session provided.");
        public static Exception ForcedSignOut()
            => new SecurityException("The Session is unavailable (forced sign-out).");
        public static Exception NotAuthenticated()
            => new SecurityException("Authenticated user required.");
    }
}
