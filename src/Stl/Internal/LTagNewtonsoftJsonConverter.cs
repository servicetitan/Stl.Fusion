using Newtonsoft.Json;

namespace Stl.Internal;

public class LTagNewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<LTag>
{
    public override void WriteJson(
        JsonWriter writer, LTag value,
        Newtonsoft.Json.JsonSerializer serializer)
        => writer.WriteValue(value.ToString());

    public override LTag ReadJson(
        JsonReader reader, Type objectType, LTag existingValue, bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
        => LTag.Parse((string?) reader.Value);
}
