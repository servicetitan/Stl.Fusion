using System;
using System.Text.Json;
using Stl.Serialization.Internal;

namespace Stl.Serialization
{
    public class SystemJsonSerializer : Utf16SerializerBase
    {
        public static JsonSerializerOptions DefaultOptions { get; set; } = new();
        public static JsonSerializerOptions ReadableOptions { get; set; } = new() { WriteIndented = true };
        public static SystemJsonSerializer Default { get; } = new(DefaultOptions);
        public static SystemJsonSerializer Readable { get; } = new(ReadableOptions);

        public JsonSerializerOptions Options { get; }

        public SystemJsonSerializer(JsonSerializerOptions? options = null)
            => Options = options ??= DefaultOptions;

        public override object? Read(string data, Type type)
            => JsonSerializer.Deserialize(data, type, Options);

        public override string Write(object? value, Type type)
            => JsonSerializer.Serialize(value, type, Options);
    }
}
