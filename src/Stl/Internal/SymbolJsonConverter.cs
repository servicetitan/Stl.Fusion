using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stl.Text;

namespace Stl.Internal
{
    public class SymbolJsonConverter : JsonConverter<Symbol>
    {
        public override Symbol Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, Symbol value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Value);
    }
}
