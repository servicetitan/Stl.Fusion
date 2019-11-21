using System;
using System.Text;

namespace Stl.Text 
{
    public sealed class ListParser
    {
        public readonly char Delimiter;
        public readonly char Escape;

        public ListParser(char delimiter, char escape = '\\')
        {
            Delimiter = delimiter;
            Escape = escape;
        }

        public void FormatItem(StringBuilder output, in ReadOnlySpan<char> item, bool isFirst)
        {
            if (!isFirst)
                output.Append(Delimiter);
            foreach (var c in item) {
                if (c == Delimiter || c == Escape)
                    output.Append(Escape);
                output.Append(c);
            }
        }

        public ReadOnlySpan<char> ParseItem(ref ReadOnlySpan<char> source)
        {
            var escape = false;
            for (var index = 0; index < source.Length; index++) {
                var c = source[index];
                if (escape)
                    escape = false;
                else if (c == Escape)
                    escape = true;
                else if (c == Delimiter) {
                    var item = source[..index]; 
                    source = source[(index + 1)..];
                    return item;
                }
            }
            return ReadOnlySpan<char>.Empty;
        }
    }
}
