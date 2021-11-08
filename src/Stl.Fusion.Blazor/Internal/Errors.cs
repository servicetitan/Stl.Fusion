namespace Stl.Fusion.Blazor.Internal;

public static class Errors
{
    public static Exception NoMatchingComponentFound(Type sourceType, string scope)
        => new ArgumentOutOfRangeException(
            $"No matching component is found for '{sourceType}' in '{scope} scope.'");
}
