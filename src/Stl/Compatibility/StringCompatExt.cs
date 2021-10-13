#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System
{
    public static class StringCompatExt
    {
        public static string[] Split(this string self,
            string? separator,
            StringSplitOptions options = StringSplitOptions.None)
            => self.Split(new[] {separator}, options);
    }
}

#endif
