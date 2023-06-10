namespace Stl.Fusion.Blazor.Internal;

public static class Errors
{
    public static Exception NoSessionId()
        => new InvalidOperationException("SessionId parameter isn't set.");
}
