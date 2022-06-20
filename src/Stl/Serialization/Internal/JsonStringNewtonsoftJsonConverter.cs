using Newtonsoft.Json;

namespace Stl.Serialization.Internal;

public class JsonStringNewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<JsonString>
{
    public override void WriteJson(
        JsonWriter writer, JsonString? value,
        Newtonsoft.Json.JsonSerializer serializer)
        => writer.WriteValue(value?.Value);

    public override JsonString? ReadJson(
        JsonReader reader, Type objectType, JsonString? existingValue, bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
        => JsonString.New((string?) reader.Value);
}
