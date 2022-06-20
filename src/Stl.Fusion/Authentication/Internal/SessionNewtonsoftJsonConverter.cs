using Newtonsoft.Json;

namespace Stl.Fusion.Authentication.Internal;

public class SessionNewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<Session>
{
    public override void WriteJson(
        JsonWriter writer, Session? value,
        Newtonsoft.Json.JsonSerializer serializer)
        => writer.WriteValue(value?.Id);

    public override Session? ReadJson(
        JsonReader reader, Type objectType, Session? existingValue, bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
    {
        var value = (string?) reader.Value;
        return value == null! ? null : new Session(value);
    }
}
