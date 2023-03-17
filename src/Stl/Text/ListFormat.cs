using Cysharp.Text;

namespace Stl.Text;

[StructLayout(LayoutKind.Auto)]
public readonly struct ListFormat
{
    public static readonly ListFormat Default = new('|');
    public static readonly ListFormat CommaSeparated = new(',');
    public static readonly ListFormat SlashSeparated = new('/');
    public static readonly ListFormat TabSeparated = new('\t');

    public readonly char Delimiter;
    public readonly char Escape;

    public ListFormat(char delimiter, char escape = '\\')
    {
        Delimiter = delimiter;
        Escape = escape;
    }

    public ListFormatter CreateFormatter(int itemIndex = 0)
        => new(this, itemIndex);

    public ListParser CreateParser(string source, int itemIndex = 0)
        => CreateParser(source.AsSpan(), itemIndex);
    public ListParser CreateParser(ReadOnlySpan<char> source, int itemIndex = 0)
        => new(this, source, itemIndex);

    public string Format(params string[] source)
    {
        using var f = CreateFormatter();
        foreach (var item in source)
            f.Append(item);
        f.AppendEnd();
        return f.Output;
    }

    public string Format(IEnumerable<string> source)
    {
        using var f = CreateFormatter();
        foreach (var item in source)
            f.Append(item);
        f.AppendEnd();
        return f.Output;
    }

    public List<string> Parse(string source, List<string>? target = null)
    {
        target ??= new List<string>();
        var p = CreateParser(source);
        while (p.TryParseNext())
            target.Add(p.Item);
        return target;
    }

    public List<string> Parse(in ReadOnlySpan<char> source, List<string>? target = null)
    {
        target ??= new List<string>();
        using var p = CreateParser(source);
        while (p.TryParseNext())
            target.Add(p.Item);
        return target;
    }
}
