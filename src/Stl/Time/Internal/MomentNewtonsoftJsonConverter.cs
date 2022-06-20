using Newtonsoft.Json;

namespace Stl.Time.Internal;

public class MomentNewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<Moment>
{
    public override void WriteJson(
        JsonWriter writer, Moment value,
        Newtonsoft.Json.JsonSerializer serializer)
        => writer.WriteValue(value.ToString());

    public override Moment ReadJson(
        JsonReader reader, Type objectType, Moment existingValue, bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
    {
        if (reader.Value is DateTime d)
            return d;
        return Moment.Parse((string?) reader.Value!);
    }
}
