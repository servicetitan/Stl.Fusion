using System;
using Newtonsoft.Json;

namespace Stl.Time.Internal
{
    public class MomentJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(Moment);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var moment = (Moment) value!;
            writer.WriteValue(moment.ToDateTime());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (DateTime) reader.Value!;
            // ReSharper disable once HeapView.BoxingAllocation
            return (Moment) value;
        }
    }
}
