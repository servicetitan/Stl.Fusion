using System;
using System.Collections.Generic;
using System.Text;
using Stl.Collections;
using Stl.Internal;

namespace Stl.Text
{
    public readonly struct ListFormat
    {
        public static readonly ListFormat Default = new('|');
        public static readonly ListFormat CommaSeparated = new(',');
        public static readonly ListFormat SlashSeparated = new('/');
        public static readonly ListFormat TabSeparated = new('\t');

        public readonly char Delimiter;
        public readonly char Escape;
        public readonly string NoItems;

        public ListFormat(char delimiter, char escape = '\\', string noItems = "[]")
        {
            Delimiter = delimiter;
            Escape = escape;
            NoItems = noItems;
        }

        public ListFormatter CreateFormatter(StringBuilder output, int itemIndex = 0)
            => new(this, output, itemIndex);

        public ListParser CreateParser(in ReadOnlySpan<char> source, StringBuilder item, int itemIndex = 0)
            => new(this, source, item, itemIndex);
    }

    public ref struct ListFormatter
    {
        public ListFormat Format => new(Delimiter, Escape, NoItems);
        public readonly char Delimiter;
        public readonly char Escape;
        public readonly string NoItems;
        public readonly StringBuilder OutputBuilder;
        public string Output => OutputBuilder.ToString();
        public int ItemIndex;

        internal ListFormatter(ListFormat format, StringBuilder outputBuilder, int itemIndex)
        {
            Delimiter = format.Delimiter;
            Escape = format.Escape;
            NoItems = format.NoItems;
            OutputBuilder = outputBuilder;
            ItemIndex = itemIndex;
        }

        public void Append(in ReadOnlySpan<char> item)
        {
            if (ItemIndex++ != 0)
                OutputBuilder.Append(Delimiter);
            foreach (var c in item) {
                if (c == Delimiter || c == Escape)
                    OutputBuilder.Append(Escape);
                OutputBuilder.Append(c);
            }
        }

        public void AppendWithEscape(in ReadOnlySpan<char> item)
        {
            if (ItemIndex++ != 0)
                OutputBuilder.Append(Delimiter);
            var isFirst = true;
            foreach (var c in item) {
                if (isFirst || c == Delimiter || c == Escape)
                    OutputBuilder.Append(Escape);
                OutputBuilder.Append(c);
                isFirst = false;
            }
        }

        public void AppendEnd()
        {
            if (ItemIndex == 0) {
                // Special case: an empty list marker
                foreach (var c in NoItems) {
                    OutputBuilder.Append(Escape);
                    OutputBuilder.Append(c);
                }
            }
        }

        public void Append(IEnumerator<string> enumerator, bool appendEndOfList = true)
        {
            while (enumerator.MoveNext())
                Append(enumerator.Current);
            if (appendEndOfList)
                AppendEnd();
        }

        public void Append(IEnumerable<string> sequence, bool appendEndOfList = true)
        {
            foreach (var item in sequence)
                Append(item);
            if (appendEndOfList)
                AppendEnd();
        }
    }

    public ref struct ListParser
    {
        public ListFormat Format => new(Delimiter, Escape, NoItems);
        public readonly char Delimiter;
        public readonly char Escape;
        public readonly string NoItems;
        public readonly StringBuilder ItemBuilder;
        public ReadOnlySpan<char> Source;
        public string Item => ItemBuilder.ToString();
        public int ItemIndex;

        internal ListParser(ListFormat format, in ReadOnlySpan<char> source, StringBuilder itemBuilder, int itemIndex)
        {
            Delimiter = format.Delimiter;
            Escape = format.Escape;
            NoItems = format.NoItems;
            Source = source;
            ItemBuilder = itemBuilder;
            ItemIndex = itemIndex;
        }

        public bool TryParseNext(bool clearItemBuilder = true)
        {
            if (clearItemBuilder)
                ItemBuilder.Clear();
            ItemIndex++;
            var startLength = ItemBuilder.Length;
            for (var index = 0; index < Source.Length; index++) {
                var c = Source[index];
                if (c == Escape) {
                    if (++index >= Source.Length) {
                        ItemBuilder.Append(Escape);
                        break;
                    }
                }
                else if (c == Delimiter) {
                    Source = Source[(index + 1)..];
                    return true;
                }
                ItemBuilder.Append(Source[index]);
            }

            if (ItemIndex == 1
                && (ItemBuilder.Length - startLength) == NoItems.Length
                && Source.Length == (NoItems.Length << 1)) {
                // Special case: possibly it's an empty list marker
                var noItems = true;
                for (var i = 1; i < Source.Length; i += 2) {
                    if (NoItems[i >> 1] != Source[i]) {
                        noItems = false;
                        break;
                    }
                }
                if (noItems) {
                    Source = Source[..0];
                    return false;
                }
            }

            Source = Source[..0];
            return ItemIndex == 1 || ItemBuilder.Length > 0;
        }

        public void ParseNext(bool clearItemBuilder = true)
        {
            if (!TryParseNext(clearItemBuilder))
                throw Errors.InvalidListFormat();
        }

        public List<string> ParseAll()
        {
            var result = new List<string>();
            while (TryParseNext())
                result.Add(Item);
            return result;
        }

        public void ParseAll(MemoryBuffer<string> buffer)
        {
            while (TryParseNext())
                buffer.Add(Item);
        }
    }
}
