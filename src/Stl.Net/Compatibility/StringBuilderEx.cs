#if NETSTANDARD2_0

using System;
using System.Text;

namespace Stl.Net
{
    internal static class StringBuilderEx
    {
        public static void Append(this StringBuilder sb, ArraySegment<char> chars)
        {
            if (sb == null) throw new ArgumentNullException(nameof(sb));
            sb.Append(chars.Array, chars.Offset, chars.Count);
        }
    }
}

#endif
