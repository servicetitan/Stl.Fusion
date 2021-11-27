namespace Stl.Extensibility;

public static class MatchingTypeFinderExt
{
    public static Type? TryFind(this IMatchingTypeFinder matchingTypeFinder, Type source)
        => matchingTypeFinder.TryFind(source, Symbol.Empty);
    public static Type? TryFind(this IMatchingTypeFinder matchingTypeFinder, Type source, Type? scope)
        => matchingTypeFinder.TryFind(source, scope?.ToSymbol() ?? "");
}
