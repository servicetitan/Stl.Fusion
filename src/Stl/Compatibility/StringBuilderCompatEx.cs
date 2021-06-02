#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System.Text
{
    public static class StringBuilderCompatEx
    {
        public static void Append(this StringBuilder sb, ArraySegment<char> chars)
        {
            if (sb == null) throw new ArgumentNullException(nameof(sb));
            sb.Append(chars.Array, chars.Offset, chars.Count);
        }
    }
}

#endif
