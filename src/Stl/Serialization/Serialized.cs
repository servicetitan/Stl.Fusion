using Cysharp.Text;
using Microsoft.Toolkit.HighPerformance;

namespace Stl.Serialization;

public enum SerializedFormat
{
    Bytes = 0,
    Text = 1,
}

[StructLayout(LayoutKind.Auto)]
public readonly record struct Serialized(
    ReadOnlyMemory<byte> Data,
    SerializedFormat Format = SerializedFormat.Bytes)
{
    public Serialized(string text) : this(text.AsMemory()) { }
    public Serialized(ReadOnlyMemory<char> text)
        : this(text.Cast<char, byte>(), SerializedFormat.Text)
    { }

    public override string ToString()
        => ToString(64);
    public string ToString(int maxLength)
    {
        var isText = IsText(out var text);
#if NET5_0_OR_GREATER
        var sData = isText
            ? new string(text.Span[..maxLength])
            : Convert.ToHexString(Data.Span[..maxLength]);
#else
        var sData = isText
            ? new string(text.Span[..maxLength].ToArray())
            : BitConverter.ToString(Data.Span[..maxLength].ToArray());
#endif
        return isText
            ? ZString.Concat("[ ", text.Length, "char(s): `", sData, maxLength <= text.Length ? "` ]" : "`... ]")
            : ZString.Concat("[ ", Data.Length, "byte(s): ", sData, maxLength <= Data.Length ? " ]" : "... ]");
    }

    public static implicit operator Serialized(ReadOnlyMemory<byte> bytes) => new(bytes);
    public static implicit operator Serialized(ReadOnlyMemory<char> text) => new(text);
    public static implicit operator Serialized(string text) => new(text);

    public bool IsText(out ReadOnlyMemory<char> text)
    {
        if (Format == SerializedFormat.Text) {
            text = Data.Cast<byte, char>();
            return true;
        }

        text = default;
        return false;
    }

    public bool IsBytes(out ReadOnlyMemory<byte> bytes)
    {
        if (Format == SerializedFormat.Text) {
            bytes = Data;
            return true;
        }

        bytes = default;
        return false;
    }
}
