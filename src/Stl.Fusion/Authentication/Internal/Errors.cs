using System.Security;
using Stl.Versioning;

namespace Stl.Fusion.Authentication.Internal;

public static class Errors
{
    public static Exception InvalidSessionId(string parameterName)
        => new ArgumentOutOfRangeException(parameterName, "Provided Session.Id is invalid.");
    public static Exception NoSessionProvided(string? parameterName = null)
        => new InvalidOperationException("No Session provided.");

    public static Exception NoSession()
        => new SecurityException("The Session is unavailable.");
    public static Exception ForcedSignOut()
        => new SecurityException("The Session is unavailable.");
    public static Exception NotAuthenticated()
        => new SecurityException("Authenticated user required.");
}
