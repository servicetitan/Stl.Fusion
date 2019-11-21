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

        public void FormatItem(StringBuilder output, in ReadOnlySpan<char> item, ref int itemIndex)
        {
            if (itemIndex != 0)
                output.Append(Delimiter);
            foreach (var c in item) {
                if (c == Delimiter || c == Escape)
                    output.Append(Escape);
                output.Append(c);
            }
            ++itemIndex;
        }

        public bool ParseItem(ref ReadOnlySpan<char> source, ref int itemIndex, StringBuilder output)
        {
            itemIndex++;
            for (var index = 0; index < source.Length; index++) {
                var c = source[index];
                if (c == Escape) {
                    if (++index >= source.Length) {
                        output.Append(Escape);
                        break;
                    }
                }
                else if (c == Delimiter) {
                    source = source[(index + 1)..];
                    return true;
                }
                output.Append(source[index]);
            }
            source = source[..0];
            return itemIndex == 1 || output.Length > 0;
        }
    }
}
