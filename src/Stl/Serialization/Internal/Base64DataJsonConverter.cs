using System;
using Newtonsoft.Json;

namespace Stl.Serialization.Internal
{
    public class Base64DataJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(Base64Data);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var data = (Base64Data) value!;
            writer.WriteValue(data.Encode());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string) reader.Value!;
            // ReSharper disable once HeapView.BoxingAllocation
            return Base64Data.Decode(value);
        }
    }
}
