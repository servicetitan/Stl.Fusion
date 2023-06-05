using Cysharp.Text;
using Microsoft.Toolkit.HighPerformance;

namespace Stl.Serialization;

public enum DataFormat
{
    Bytes = 0,
    Text = 1,
}

[DataContract]
[StructLayout(LayoutKind.Auto)]
public readonly record struct TextOrBytes(
    [property: DataMember(Order = 0)]
    DataFormat Format,
    [property: JsonIgnore, Newtonsoft.Json.JsonIgnore]
    ReadOnlyMemory<byte> Data)
{
    public static readonly TextOrBytes EmptyBytes = new(DataFormat.Bytes, default!);
    public static readonly TextOrBytes EmptyText = new(DataFormat.Text, default!);

    [DataMember(Order = 1)]
    public byte[] Bytes => Data.ToArray();
    public bool IsEmpty => Data.Length == 0;

    public TextOrBytes(string text)
        : this(text.AsMemory()) { }
    public TextOrBytes(ReadOnlyMemory<char> text)
        : this(DataFormat.Text, text.Cast<char, byte>()) { }
    public TextOrBytes(ReadOnlyMemory<byte> bytes)
        : this(DataFormat.Bytes, bytes) { }
    [JsonConstructor, Newtonsoft.Json.JsonConstructor]
    public TextOrBytes(DataFormat format, byte[] bytes)
        : this(format, bytes.AsMemory()) { }

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

    public static implicit operator TextOrBytes(ReadOnlyMemory<byte> bytes) => new(bytes);
    public static implicit operator TextOrBytes(ReadOnlyMemory<char> text) => new(text);
    public static implicit operator TextOrBytes(string text) => new(text);

    public bool IsText(out ReadOnlyMemory<char> text)
    {
        if (Format == DataFormat.Text) {
            text = Data.Cast<byte, char>();
            return true;
        }

        text = default;
        return false;
    }

    public bool IsBytes(out ReadOnlyMemory<byte> bytes)
    {
        if (Format == DataFormat.Text) {
            bytes = Data;
            return true;
        }

        bytes = default;
        return false;
    }
}
