using System;
using System.Collections.Generic;
using Cysharp.Text;

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

        public ListFormatter CreateFormatter(int itemIndex = 0)
            => new(this, ZString.CreateStringBuilder(), true, itemIndex);
        public ListFormatter CreateFormatter(in Utf16ValueStringBuilder output, int itemIndex = 0)
            => new(this, output, false, itemIndex);

        public ListParser CreateParser(in string source, int itemIndex = 0)
            => CreateParser(source.AsSpan(), itemIndex);
        public ListParser CreateParser(in string source, in Utf16ValueStringBuilder item, int itemIndex = 0)
            => CreateParser(source.AsSpan(), item, itemIndex);

        public ListParser CreateParser(in ReadOnlySpan<char> source, int itemIndex = 0)
            => new(this, source, ZString.CreateStringBuilder(), true, itemIndex);
        public ListParser CreateParser(in ReadOnlySpan<char> source, in Utf16ValueStringBuilder item, int itemIndex = 0)
            => new(this, source, item, false, itemIndex);

        public string Format(params string[] source) => Format((IEnumerable<string>) source);
        public string Format(IEnumerable<string> source)
        {
            using var f = CreateFormatter();
            foreach (var item in source)
                f.Append(item);
            f.AppendEnd();
            f.AppendEnd();
            return f.Output;
        }

        public List<string> Parse(in ReadOnlySpan<char> source)
        {
            var result = new List<string>();
            var p = CreateParser(source, ZString.CreateStringBuilder());
            try {
                while (p.TryParseNext())
                    result.Add(p.Item);
                return result;
            }
            finally {
                p.ItemBuilder.Dispose();
            }
        }
    }
}
