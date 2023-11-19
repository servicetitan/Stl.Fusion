using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;
using Stl.Serialization.Internal;

namespace Stl.Serialization;

public class SystemJsonSerializer : TextSerializerBase
{
    public static JsonSerializerOptions PrettyOptions { get; set; } = new() { WriteIndented = true };
    public static JsonSerializerOptions DefaultOptions { get; set; } = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    public static SystemJsonSerializer Pretty { get; set; } = new(PrettyOptions);
    public static SystemJsonSerializer Default { get; set; } = new(DefaultOptions);

    public JsonSerializerOptions Options { get; }

    public SystemJsonSerializer() : this(DefaultOptions) { }
    public SystemJsonSerializer(JsonSerializerOptions options)
    {
        Options = options;
        PreferStringApi = false;
    }

    // Read

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override object? Read(string data, Type type)
        => JsonSerializer.Deserialize(data, type, Options);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        readLength = data.Length;
        var utf8JsonReader = new Utf8JsonReader(data.Span);
        return JsonSerializer.Deserialize(ref utf8JsonReader, type, Options);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override object? Read(ReadOnlyMemory<char> data, Type type)
        => JsonSerializer.Deserialize(data.Span, type, Options);

    // Write

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override string Write(object? value, Type type)
        => JsonSerializer.Serialize(value, type, Options);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    {
        var utf8JsonWriter = new Utf8JsonWriter(bufferWriter);
        JsonSerializer.Serialize(utf8JsonWriter, value, type, Options);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override void Write(TextWriter textWriter, object? value, Type type)
    {
        var result = JsonSerializer.Serialize(value, type, Options);
        textWriter.Write(result);
    }
}
