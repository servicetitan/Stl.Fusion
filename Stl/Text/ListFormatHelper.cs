using System;
using System.Text;

namespace Stl.Text 
{
    public readonly struct ListFormatHelper
    {
        public static readonly ListFormatHelper Default = new ListFormatHelper('|');
        public static readonly ListFormatHelper CommaSeparated = new ListFormatHelper(',');
        public static readonly ListFormatHelper TabSeparated = new ListFormatHelper('\t');

        public readonly char Delimiter;
        public readonly char Escape;

        public ListFormatHelper(char delimiter, char escape = '\\')
        {
            Delimiter = delimiter;
            Escape = escape;
        }

        public ListFormatter CreateFormatter(StringBuilder? output = null, int itemIndex = 0)
            => new ListFormatter(this, output ?? new StringBuilder(), itemIndex);

        public ListParser CreateParser(in ReadOnlySpan<char> source, StringBuilder? item = null, int itemIndex = 0)
            => new ListParser(this, source, item ?? new StringBuilder(), itemIndex);
    }

    public ref struct ListFormatter
    {
        public ListFormatHelper Helper => new ListFormatHelper(Delimiter, Escape);
        public readonly char Delimiter;
        public readonly char Escape;
        public readonly StringBuilder OutputBuilder;
        public string Output => OutputBuilder.ToString();
        public int ItemIndex;

        internal ListFormatter(ListFormatHelper helper, StringBuilder outputBuilder, int itemIndex)
        {
            Delimiter = helper.Delimiter;
            Escape = helper.Escape;
            OutputBuilder = outputBuilder;
            ItemIndex = itemIndex;
        }

        public void AddItem(in ReadOnlySpan<char> item)
        {
            if (ItemIndex++ != 0)
                OutputBuilder.Append(Delimiter);
            foreach (var c in item) {
                if (c == Delimiter || c == Escape)
                    OutputBuilder.Append(Escape);
                OutputBuilder.Append(c);
            }
        }

        public void AddItemWithExtraEscape(in ReadOnlySpan<char> item)
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

        public void AddEnd()
        {
            if (ItemIndex == 0) {
                // Special case: a single escape in the output
                // means there is zero items.
                OutputBuilder.Append(Escape);
            }
        }
    }

    public ref struct ListParser
    {
        public ListFormatHelper Helper => new ListFormatHelper(Delimiter, Escape);
        public readonly char Delimiter;
        public readonly char Escape;
        public ReadOnlySpan<char> Source;
        public StringBuilder ItemBuilder;
        public string Item => ItemBuilder.ToString();
        public int ItemIndex;

        internal ListParser(ListFormatHelper helper, in ReadOnlySpan<char> source, StringBuilder itemBuilder, int itemIndex)
        {
            Delimiter = helper.Delimiter;
            Escape = helper.Escape;
            Source = source;
            ItemBuilder = itemBuilder;
            ItemIndex = itemIndex;
        }

        public bool ClearAndParseItem()
        {
            ItemBuilder.Clear();
            return ParseItem();
        }

        public bool ParseItem()
        {
            ItemIndex++;
            for (var index = 0; index < Source.Length; index++) {
                var c = Source[index];
                if (c == Escape) {
                    if (++index >= Source.Length) {
                        if (ItemIndex == 1) {
                            // Special case: a single escape in the output
                            // means there is zero items.
                            Source = Source[..0];
                            return false;
                        }
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
            Source = Source[..0];
            return ItemIndex == 1 || ItemBuilder.Length > 0;
        }
    }
}
