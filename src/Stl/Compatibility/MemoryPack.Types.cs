// Source (with light edits):
// - https://github.com/Cysharp/MemoryPack/blob/main/src/MemoryPack.Core/MemoryPackSerializerOptions.cs

#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace MemoryPack;

public record MemoryPackSerializerOptions
{
    // Default is Utf8
    public static readonly MemoryPackSerializerOptions Default = new() { StringEncoding = StringEncoding.Utf8 };
    public static readonly MemoryPackSerializerOptions Utf8 = Default with { StringEncoding = StringEncoding.Utf8 };
    public static readonly MemoryPackSerializerOptions Utf16 = Default with { StringEncoding = StringEncoding.Utf16 };

    public StringEncoding StringEncoding { get; init; }
    public IServiceProvider? ServiceProvider { get; init; }
}

#pragma warning disable CA1028
public enum StringEncoding : byte
#pragma warning restore CA1028
{
    Utf16,
    Utf8,
}

#endif
