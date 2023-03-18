using Cysharp.Text;
using Stl.Internal;

namespace Stl.Text;

[StructLayout(LayoutKind.Auto)]
public ref struct ListParser
{
    public ListFormat Format => new(Delimiter, Escape);
    public readonly char Delimiter;
    public readonly char Escape;
    public ReadOnlySpan<char> Source;
    public Utf16ValueStringBuilder ItemBuilder;
    public int ItemIndex;
    public string Item => ItemBuilder.ToString();

#pragma warning disable RCS1242
    internal ListParser(
        ListFormat format,
        ReadOnlySpan<char> source,
        int itemIndex)
#pragma warning restore RCS1242
    {
        Delimiter = format.Delimiter;
        Escape = format.Escape;
        Source = source;
        ItemBuilder = ZString.CreateStringBuilder();
        ItemIndex = itemIndex;
    }

    public void Dispose()
        => ItemBuilder.Dispose();

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
                    if (ItemIndex == 1 && ItemBuilder.Length == 0) {
                        // Special case: single Escape = an empty list
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
