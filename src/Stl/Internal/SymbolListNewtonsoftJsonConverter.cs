using System;
using Newtonsoft.Json;
using Stl.Text;

namespace Stl.Internal
{
    public class SymbolListNewtonsoftJsonConverter : JsonConverter<SymbolList>
    {
        public override void WriteJson(JsonWriter writer, SymbolList? value, JsonSerializer serializer)
            => writer.WriteValue(value?.FormattedValue);

        public override SymbolList? ReadJson(JsonReader reader,
            Type objectType, SymbolList? existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var value = (string?) reader.Value;
            return value == null ? null : SymbolList.Parse(value);
        }
    }
}
