using System;
using System.Text.Json;

namespace Stl.Serialization
{
    public class DefaultJsonSerializer : SerializerBase<string>
    {
        public static JsonSerializerOptions DefaultOptions { get; set; } = new();

        public JsonSerializerOptions Options { get; }

        public DefaultJsonSerializer(JsonSerializerOptions? options = null)
            => Options = options ??= DefaultOptions;

        public override string Serialize(object? native, Type? type)
        {
            type ??= native?.GetType() ?? typeof(object);
            return JsonSerializer.Serialize(native, type, Options);
        }

        public override object? Deserialize(string serialized, Type? type)
        {
            type ??= typeof(object);
            return JsonSerializer.Deserialize(serialized, type, Options);
        }
    }
}
