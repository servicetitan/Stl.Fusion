using System;

namespace Stl.Fusion.Authentication.Internal
{
    public static class Errors
    {
        public static Exception NoAuthContext(string? parameterName = null)
            => new InvalidOperationException("No AuthContext.");
    }
}
