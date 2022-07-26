namespace Stl.Versioning;

public static class VersionChecker
{
    public static bool IsExpected<TVersion>(TVersion version, TVersion? expectedVersion)
        => expectedVersion is not { } expected
            || EqualityComparer<TVersion>.Default.Equals(version, expected);

    public static TVersion RequireExpected<TVersion>(TVersion version, TVersion? expectedVersion)
        => IsExpected(version, expectedVersion)
            ? version
            : throw new VersionMismatchException();
}
