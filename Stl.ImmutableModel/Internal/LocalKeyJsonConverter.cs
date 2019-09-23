using System;
using Newtonsoft.Json;

namespace Stl.ImmutableModel.Internal
{
    public class LocalKeyJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) 
            => objectType == typeof(LocalKey);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var typeRef = (LocalKey) value!;
            writer.WriteValue(typeRef.Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string) reader.Value!;
            // ReSharper disable once HeapView.BoxingAllocation
            return new LocalKey(value);
        }
    }
}
