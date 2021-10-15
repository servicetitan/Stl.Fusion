using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stl.Time.Internal;

public class MomentJsonConverter : JsonConverter<Moment>
{
    public override Moment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Moment.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, Moment value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
