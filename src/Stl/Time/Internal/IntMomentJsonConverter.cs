using System;
using Newtonsoft.Json;

namespace Stl.Time.Internal
{
    public class IntMomentJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) 
            => objectType == typeof(IntMoment);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var moment = (IntMoment) value!;
            writer.WriteValue(moment.ToDateTime());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (DateTime) reader.Value!;
            // ReSharper disable once HeapView.BoxingAllocation
            return (IntMoment) value;
        }
    }
}
