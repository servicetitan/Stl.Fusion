using System;
using Newtonsoft.Json;

namespace Stl.Fusion.Client.Internal
{
    public class JsonStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(JsonString);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var rs = (JsonString) value!;
            writer.WriteValue(rs.Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string) reader.Value!;
            return new JsonString(value);
        }
    }
}
