using Newtonsoft.Json;

namespace Stl.IO.Internal;

public class FilePathNewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<FilePath>
{
    public override void WriteJson(
        JsonWriter writer, FilePath value,
        Newtonsoft.Json.JsonSerializer serializer)
        => writer.WriteValue(value.Value);

    public override FilePath ReadJson(
        JsonReader reader, Type objectType, FilePath existingValue, bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
        => new((string?) reader.Value);
}
