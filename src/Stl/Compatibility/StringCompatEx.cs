#if NETSTANDARD2_0

namespace System
{
    public static class StringCompatEx
    {
        public static string[] Split(this string self,
            string? separator,
            StringSplitOptions options = StringSplitOptions.None)
            => self.Split(new[] {separator}, options);
    }
}

#endif
