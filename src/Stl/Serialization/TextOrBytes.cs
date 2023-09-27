using Cysharp.Text;
using Microsoft.Toolkit.HighPerformance;

namespace Stl.Serialization;

public enum DataFormat
{
    Bytes = 0,
    Text = 1,
}

[StructLayout(LayoutKind.Auto)]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public readonly partial record struct TextOrBytes(
    [property: DataMember(Order = 0), MemoryPackOrder(0)]
    DataFormat Format,
    [property: JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    ReadOnlyMemory<byte> Data)
{
    public static readonly TextOrBytes EmptyBytes = new(DataFormat.Bytes, default!);
    public static readonly TextOrBytes EmptyText = new(DataFormat.Text, default!);

    private readonly byte[]? _data; // This field is used solely to avoid .ToArray() calls in Bytes property

    [DataMember(Order = 1), MemoryPackOrder(1)]
    public byte[] Bytes => _data ?? Data.ToArray();

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember, MemoryPackIgnore]
    public bool IsEmpty => Data.Length == 0;

    public TextOrBytes(string text)
        : this(text.AsMemory()) { }
    public TextOrBytes(byte[] bytes)
        : this(DataFormat.Bytes, bytes.AsMemory())
        => _data = bytes;
    public TextOrBytes(ReadOnlyMemory<char> text)
        : this(DataFormat.Text, text.Cast<char, byte>()) { }
    public TextOrBytes(ReadOnlyMemory<byte> bytes)
        : this(DataFormat.Bytes, bytes) { }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public TextOrBytes(DataFormat format, byte[] bytes)
        : this(format, bytes.AsMemory()) { }

    public override string ToString()
        => ToString(64);
    public string ToString(int maxLength)
    {
        var isText = IsText(out var text);
#if NET5_0_OR_GREATER
        var sData = isText
            ? new string(text.Span[..Math.Min(text.Length, maxLength)])
            : Convert.ToHexString(Data.Span[..Math.Min(Data.Length, maxLength)]);
#else
        var sData = isText
            ? new string(text.Span[..Math.Min(text.Length, maxLength)].ToArray())
            : BitConverter.ToString(Data.Span[..Math.Min(Data.Length, maxLength)].ToArray());
#endif
        return isText
            ? ZString.Concat("[ ", text.Length, " char(s): `", sData, maxLength < text.Length ? "` ]" : "`... ]")
            : ZString.Concat("[ ", Data.Length, " byte(s): ", sData, maxLength < Data.Length ? " ]" : "... ]");
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
        if (Format == DataFormat.Bytes) {
            bytes = Data;
            return true;
        }

        bytes = default;
        return false;
    }

    public TextOrBytes CopyData()
        => new(Format, Data.ToArray());

    // Structural equality

    public int GetDataHashCode()
        => Data.Span.GetDjb2HashCode();

    public bool DataEquals(TextOrBytes other)
        => Data.Span.SequenceEqual(other.Data.Span);
}
