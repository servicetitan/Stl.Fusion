using System.Text.Json;
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
        => Options = options ??= DefaultOptions;

    public override object? Read(string data, Type type)
        => JsonSerializer.Deserialize(data, type, Options);

    public override string Write(object? value, Type type)
        => JsonSerializer.Serialize(value, type, Options);
}
