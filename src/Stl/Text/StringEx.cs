using System;
using System.Collections.Generic;
using Stl.Collections;

namespace Stl.Text
{
    public static class StringEx
    {
        public static string ManyToOne(IEnumerable<string> values) => ManyToOne(values, ListFormat.Default);
        public static string ManyToOne(IEnumerable<string> values, ListFormat listFormat)
        {
            var formatter = listFormat.CreateFormatter(StringBuilderEx.Acquire());
            foreach (var value in values)
                formatter.Append(value);
            formatter.AppendEnd();
            return formatter.OutputBuilder.ToStringAndRelease();
        }

        public static string[] OneToMany(string value) => OneToMany(value, ListFormat.Default);
        public static string[] OneToMany(string value, ListFormat listFormat)
        {
            if (value == "")
                return Array.Empty<string>();
            var parser = listFormat.CreateParser(value, StringBuilderEx.Acquire());
            var buffer = MemoryBuffer<string>.Lease(true);
            try {
                while (parser.TryParseNext()) {
                    buffer.Add(parser.Item);
                }
                return buffer.ToArray();
            }
            finally {
                buffer.Release();
                parser.ItemBuilder.Release();
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
