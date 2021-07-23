using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stl.Text;

namespace Stl.Internal
{
    public class SymbolListJsonConverter : JsonConverter<SymbolList>
    {
        public override SymbolList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => SymbolList.Parse(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, SymbolList value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.FormattedValue);
    }
}
