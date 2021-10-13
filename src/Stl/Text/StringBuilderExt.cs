using System;
using System.Text;

namespace Stl.Text
{
    // StringBuilder caching.
    // Prefer ZString.CreateStringBuilder instead, if possible.
    // See https://referencesource.microsoft.com/#mscorlib/system/text/stringbuildercache.cs,a6dbe82674916ac0
    public static class StringBuilderExt
    {
        private const int MaxCapacity = 2048;
        [ThreadStatic]
        private static StringBuilder? _cached;

        public static StringBuilder Acquire(int capacity = 0x10)
        {
            if (capacity <= MaxCapacity) {
                var sb = _cached;
                if (sb != null && sb.Capacity >= capacity) {
                    _cached = null;
                    return sb;
                }
            }
            return new StringBuilder(capacity);
        }

        public static void Release(this StringBuilder sb)
        {
            if (sb.Capacity > MaxCapacity)
                return;
            sb.Clear();
            _cached = sb;
        }

        public static string ToStringAndRelease(this StringBuilder sb)
        {
            var result = sb.ToString();
            Release(sb);
            return result;
        }

        public static string ToStringAndRelease(this StringBuilder sb, int start, int length)
        {
            var result = sb.ToString(start, length);
            Release(sb);
            return result;
        }
    }
}
