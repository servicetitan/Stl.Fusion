using System;
using Newtonsoft.Json;
using Stl.Text;

namespace Stl.Internal
{
    public class SymbolJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) 
            => objectType == typeof(Symbol);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var symbol = (Symbol) value!;
            writer.WriteValue(symbol.Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string) reader.Value!;
            // ReSharper disable once HeapView.BoxingAllocation
            return new Symbol(value);
        }
    }
}
