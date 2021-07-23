using System;
using Newtonsoft.Json;

namespace Stl.Serialization.Internal
{
    public class JsonStringNewtonsoftJsonConverter : JsonConverter<JsonString>
    {
        public override void WriteJson(JsonWriter writer, JsonString? value, JsonSerializer serializer)
            => writer.WriteValue(value?.Value);

        public override JsonString? ReadJson(JsonReader reader, Type objectType,
            JsonString? existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => new((string?) reader.Value);
    }
}
