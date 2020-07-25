using System;
using Newtonsoft.Json;

namespace Stl.Internal
{
    public class LTagJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(LTag);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var moment = (LTag) value!;
            writer.WriteValue(moment.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string) reader.Value!;
            // ReSharper disable once HeapView.BoxingAllocation
            return LTag.Parse(value);
        }
    }
}
