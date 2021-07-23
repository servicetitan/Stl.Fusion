using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stl.IO;

namespace Stl.Internal
{
    public class PathStringJsonConverter : JsonConverter<PathString>
    {
        public override PathString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString());

        public override void Write(Utf8JsonWriter writer, PathString value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Value);
    }
}
