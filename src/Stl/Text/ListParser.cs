using System;
using System.Collections.Generic;
using Cysharp.Text;
using Stl.Collections;
using Stl.Internal;

namespace Stl.Text
{
    public ref struct ListParser
    {
        public ListFormat Format => new(Delimiter, Escape, NoItems);
        public readonly char Delimiter;
        public readonly char Escape;
        public readonly string NoItems;
        public ReadOnlySpan<char> Source;
        public Utf16ValueStringBuilder ItemBuilder;
        public readonly bool OwnsItemBuilder;
        public int ItemIndex;
        public string Item => ItemBuilder.ToString();

        internal ListParser(
            ListFormat format,
            in ReadOnlySpan<char> source,
            in Utf16ValueStringBuilder itemBuilder,
            bool ownsItemBuilder,
            int itemIndex)
        {
            Delimiter = format.Delimiter;
            Escape = format.Escape;
            NoItems = format.NoItems;
            Source = source;
            ItemBuilder = itemBuilder;
            OwnsItemBuilder = ownsItemBuilder;
            ItemIndex = itemIndex;
        }

        public void Dispose()
        {
            if (OwnsItemBuilder)
                ItemBuilder.Dispose();
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
