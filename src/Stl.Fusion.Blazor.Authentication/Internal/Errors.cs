namespace Stl.Fusion.Blazor.Authentication.Internal;

public static class Errors
{
    public static Exception NoSessionId()
        => new InvalidOperationException("SessionId parameter isn't set.");
}
