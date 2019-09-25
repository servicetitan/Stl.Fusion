using System;
using Newtonsoft.Json;

namespace Stl.Internal
{
    public class SymbolListJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) 
            => objectType == typeof(SymbolList);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var typeRef = (SymbolList) value!;
            writer.WriteValue(typeRef.FormattedValue);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string) reader.Value!;
            // ReSharper disable once HeapView.BoxingAllocation
            return SymbolList.Parse(value);
        }
    }
}
