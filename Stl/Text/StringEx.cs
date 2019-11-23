using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Stl.Collections;

namespace Stl.Text
{
    public static class StringEx
    {
        private static readonly ListFormat OneToManyListFormat = new ListFormat('|'); 

        public static string ManyToOne(IEnumerable<string> values)
        {
            var formatter = OneToManyListFormat.CreateFormatter();
            foreach (var value in values)
                formatter.Append(value);
            formatter.AppendEnd();
            return formatter.Output;
        }

        public static string[] OneToMany(string value)
        {
            if (value == "")
                return Array.Empty<string>();
            var parser = OneToManyListFormat.CreateParser(value);
            var buffer = ListBuffer<string>.Lease();
            try {
                while (parser.ClearAndTryParseNext()) {
                    buffer.Add(parser.Item);
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
