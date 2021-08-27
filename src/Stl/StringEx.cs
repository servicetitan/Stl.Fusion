using System.Runtime.CompilerServices;

namespace Stl
{
    public static class StringEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty(this string? source)
            => string.IsNullOrEmpty(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? NullIfEmpty(this string? source)
            => string.IsNullOrEmpty(source) ? null : source;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? NullIfWhiteSpace(this string? source)
            => string.IsNullOrWhiteSpace(source) ? null : source;
    }
}
