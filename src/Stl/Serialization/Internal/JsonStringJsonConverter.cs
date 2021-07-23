using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stl.Serialization.Internal
{
    public class JsonStringJsonConverter : JsonConverter<JsonString>
    {
        public override JsonString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString());

        public override void Write(Utf8JsonWriter writer, JsonString value, JsonSerializerOptions options)
            => writer.WriteStringValue(value?.Value);
    }
}
