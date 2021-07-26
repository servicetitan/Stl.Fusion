using System;
using Newtonsoft.Json;

namespace Stl.IO.Internal
{
    public class PathStringNewtonsoftJsonConverter : JsonConverter<PathString>
    {
        public override void WriteJson(JsonWriter writer, PathString value, JsonSerializer serializer)
            => writer.WriteValue(value.Value);

        public override PathString ReadJson(JsonReader reader, Type objectType,
            PathString existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => new((string?) reader.Value);
    }
}
