using System.Buffers;
using Stl.Serialization.Internal;

namespace Stl.Serialization;

public class SystemJsonSerializer : TextSerializerBase
{
    public static JsonSerializerOptions DefaultOptions { get; set; } = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    public static SystemJsonSerializer Default { get; } = new(DefaultOptions);

    public JsonSerializerOptions Options { get; }

    public SystemJsonSerializer(JsonSerializerOptions? options = null)
    {
        Options = options ??= DefaultOptions;
        PreferStringApi = false;
    }

    public override object? Read(string data, Type type)
        => JsonSerializer.Deserialize(data, type, Options);
    public override object? Read(ReadOnlyMemory<byte> data, Type type)
        => JsonSerializer.Deserialize(data.Span, type);

    public override string Write(object? value, Type type)
        => JsonSerializer.Serialize(value, type, Options);
    public override void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
        => JsonSerializer.Serialize(new Utf8JsonWriter(bufferWriter), type, Options);
}
