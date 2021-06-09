using System;
using System.Text.Json;

namespace Stl.Serialization
{
    public class DefaultJsonSerializer : Utf16SerializerBase
    {
        public static JsonSerializerOptions DefaultOptions { get; set; } = new();

        public JsonSerializerOptions Options { get; }

        public DefaultJsonSerializer(JsonSerializerOptions? options = null)
            => Options = options ??= DefaultOptions;

        public override object? Read(string data, Type type)
            => JsonSerializer.Deserialize(data, type, Options);

        public override string Write(object? value, Type type)
            => JsonSerializer.Serialize(value, type, Options);
    }
}
