using Cysharp.Text;

namespace Stl.Text;

[StructLayout(LayoutKind.Auto)]
public ref struct ListFormatter
{
    public ListFormat Format => new(Delimiter, Escape);
    public readonly char Delimiter;
    public readonly char Escape;
    public Utf16ValueStringBuilder OutputBuilder;
    public int ItemIndex;
    public string Output => OutputBuilder.ToString();

#pragma warning disable RCS1242
    internal ListFormatter(
        ListFormat format,
        int itemIndex)
#pragma warning restore RCS1242
    {
        Delimiter = format.Delimiter;
        Escape = format.Escape;
        OutputBuilder = ZString.CreateStringBuilder();
        ItemIndex = itemIndex;
    }

    public void Dispose()
        => OutputBuilder.Dispose();

    public void Append(string item)
    {
        if (ItemIndex++ != 0)
            OutputBuilder.Append(Delimiter);
        foreach (var c in item) {
            if (c == Delimiter || c == Escape)
                OutputBuilder.Append(Escape);
            OutputBuilder.Append(c);
        }
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
        if (ItemIndex == 0)
            // Special case: single Escape = an empty list
            OutputBuilder.Append(Escape);
    }

    public void Append(IEnumerator<string> enumerator, bool appendEndOfList = true)
    {
        while (enumerator.MoveNext())
            Append(enumerator.Current!);
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
