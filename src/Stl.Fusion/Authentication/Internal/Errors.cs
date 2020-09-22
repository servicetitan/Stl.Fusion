using System;

namespace Stl.Fusion.Authentication.Internal
{
    public static class Errors
    {
        public static Exception NoSessionProvided(string? parameterName = null)
            => new InvalidOperationException("No Session provided.");
    }
}
