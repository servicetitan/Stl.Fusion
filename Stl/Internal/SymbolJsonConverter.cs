using System;
using Newtonsoft.Json;

namespace Stl.Internal
{
    public class SymbolJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) 
            => objectType == typeof(Symbol);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var typeRef = (Symbol) value!;
            writer.WriteValue(typeRef.Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string) reader.Value!;
            // ReSharper disable once HeapView.BoxingAllocation
            return new Symbol(value);
        }
    }
}
