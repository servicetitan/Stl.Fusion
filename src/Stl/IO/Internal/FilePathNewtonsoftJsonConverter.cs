using System;
using Newtonsoft.Json;

namespace Stl.IO.Internal
{
    public class FilePathNewtonsoftJsonConverter : JsonConverter<FilePath>
    {
        public override void WriteJson(JsonWriter writer, FilePath value, JsonSerializer serializer)
            => writer.WriteValue(value.Value);

        public override FilePath ReadJson(JsonReader reader, Type objectType,
            FilePath existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => new((string?) reader.Value);
    }
}
