using System.Buffers;
using Stl.Serialization.Internal;

namespace Stl.Serialization;

public class SystemJsonSerializer : TextSerializerBase
{
    public static readonly JsonSerializerOptions PrettyOptions = new() { WriteIndented = true };
    public static readonly JsonSerializerOptions DefaultOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    public static readonly SystemJsonSerializer Pretty = new(PrettyOptions);
    public static readonly SystemJsonSerializer Default = new(DefaultOptions);

    public JsonSerializerOptions Options { get; }

    public SystemJsonSerializer() : this(DefaultOptions) { }
    public SystemJsonSerializer(JsonSerializerOptions options)
    {
        Options = options;
        PreferStringApi = false;
    }

    // Read

    public override object? Read(string data, Type type)
        => JsonSerializer.Deserialize(data, type, Options);
    public override object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        readLength = data.Length;
        var utf8JsonReader = new Utf8JsonReader(data.Span);
        return JsonSerializer.Deserialize(ref utf8JsonReader, type, Options);
    }

    public override object? Read(ReadOnlyMemory<char> data, Type type)
        => JsonSerializer.Deserialize(data.Span, type, Options);

    // Write

    public override string Write(object? value, Type type)
        => JsonSerializer.Serialize(value, type, Options);

    public override void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    {
        var utf8JsonWriter = new Utf8JsonWriter(bufferWriter);
        JsonSerializer.Serialize(utf8JsonWriter, value, type, Options);
    }

    public override void Write(TextWriter textWriter, object? value, Type type)
    {
        var result = JsonSerializer.Serialize(value, type, Options);
        textWriter.Write(result);
    }
}
