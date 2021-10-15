using Newtonsoft.Json;

namespace Stl.Fusion.Authentication.Internal;

public class SessionNewtonsoftJsonConverter : JsonConverter<Session>
{
    public override void WriteJson(JsonWriter writer, Session? value, JsonSerializer serializer)
        => writer.WriteValue(value?.Id);

    public override Session? ReadJson(JsonReader reader, Type objectType,
        Session? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var value = (string?) reader.Value;
        return value == null! ? null : new Session(value);
    }
}
