using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stl.Fusion.Authentication.Internal;

public class SessionJsonConverter : JsonConverter<Session?>
{
    public override Session? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value == null! ? null : new Session(value);
    }

    public override void Write(Utf8JsonWriter writer, Session? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value?.Id);
}
