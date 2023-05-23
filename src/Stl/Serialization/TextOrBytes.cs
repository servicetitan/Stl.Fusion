using System.Diagnostics.CodeAnalysis;

namespace Stl.Serialization;

public readonly record struct TextOrBytes(
    string? Text,
    ReadOnlyMemory<byte> Bytes)
{
    public TextOrBytes(string text)
        : this(text, ReadOnlyMemory<byte>.Empty)
    { }

    public TextOrBytes(ReadOnlyMemory<byte> bytes)
        : this(null, bytes)
    { }

    public override string ToString()
        => Text != null
            ? $"[ {Text.Length} char(s): {JsonFormatter.Format(Text)} ]"
#if NET5_0_OR_GREATER
            : $"[ {Bytes.Length} byte(s): 0x{Convert.ToHexString(Bytes.Span)} ]";
#else            
            : $"{{ {Bytes.Length} byte(s): 0x{BitConverter.ToString(Bytes.ToArray())} }}";
#endif

#if NETSTANDARD2_0
    public bool IsText(out string? text)
#else
    public bool IsText([NotNullWhen(true)] out string? text)
#endif
    {
        text = Text;
        return Text != null;
    }

    public bool IsBytes(out ReadOnlyMemory<byte> bytes)
    {
        bytes = Bytes;
        return Text == null;
    }
}
