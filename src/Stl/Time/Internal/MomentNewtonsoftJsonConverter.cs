using Newtonsoft.Json;

namespace Stl.Time.Internal;

public class MomentNewtonsoftJsonConverter : JsonConverter<Moment>
{
    public override void WriteJson(JsonWriter writer, Moment value, JsonSerializer serializer)
        => writer.WriteValue(value.ToString());

    public override Moment ReadJson(
        JsonReader reader, Type objectType,
        Moment existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.Value is DateTime d)
            return d;
        return Moment.Parse((string?) reader.Value!);
    }
}
