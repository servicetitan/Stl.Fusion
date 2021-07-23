using System;
using Newtonsoft.Json;

namespace Stl.Internal
{
    public class LTagNewtonsoftJsonConverter : JsonConverter<LTag>
    {
        public override void WriteJson(JsonWriter writer, LTag value, JsonSerializer serializer)
            => writer.WriteValue(value.ToString());

        public override LTag ReadJson(JsonReader reader,
            Type objectType, LTag existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => LTag.Parse((string?) reader.Value);
    }
}
