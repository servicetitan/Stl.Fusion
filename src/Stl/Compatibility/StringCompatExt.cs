#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System;

public static class StringCompatExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string self, char value, StringComparison comparison)
        => self.Contains(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOf(this string self, char value, StringComparison comparison)
        => self.IndexOf(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWith(this string self, char value)
        => self.EndsWith(value.ToString(), StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] Split(this string self,
        string? separator,
        StringSplitOptions options = StringSplitOptions.None)
        => self.Split(new[] {separator}, options);
}

#endif
