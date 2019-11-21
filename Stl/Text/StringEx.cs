using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Stl.Collections;

namespace Stl.Text
{
    public static class StringEx
    {
        private static readonly ListParser OneToManyParser = new ListParser('|'); 

        public static string ManyToOne(IEnumerable<string> values)
        {
            var sb = new StringBuilder();
            var index = 0;
            foreach (var value in values)
                OneToManyParser.FormatItem(sb, value, ref index);
            return sb.ToString();
        }

        public static string[] OneToMany(string value)
        {
            if (value == "")
                return Array.Empty<string>();
            var tail = value.AsSpan();
            var buffer = ListBuffer<string>.Lease();
            try {
                var index = 0;
                var item = new StringBuilder();
                while (OneToManyParser.ParseItem(ref tail, ref index, item)) {
                    buffer.Add(item.ToString());
                    item.Clear();
                }
                return buffer.ToArray();
            }
            finally {
                buffer.Release();
            }
        }

        public static int GetDeterministicHashCode(this string source)
        {
            unchecked {
                var hash1 = (5381 << 16) + 5381;
                var hash2 = hash1;
                for (var i = 0; i < source.Length; i += 2) {
                    hash1 = ((hash1 << 5) + hash1) ^ source[i];
                    if (i == source.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ source[i + 1];
                }
                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
