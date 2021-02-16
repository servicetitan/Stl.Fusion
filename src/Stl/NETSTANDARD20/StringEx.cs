#if NETSTANDARD2_0

namespace System
{
    public static class StringEx
    {
        public static string Create(ReadOnlySpan<char> slice)
        {
            return new string(slice.ToArray());
        }

        public static string[] Split(this string self, string? separator,
            StringSplitOptions options = StringSplitOptions.None)
        {
            return self.Split(new[] {separator}, options);
        }
    }
}

#endif