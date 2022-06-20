namespace Stl.Internal;

public class LTagJsonConverter : JsonConverter<LTag>
{
    public override LTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => LTag.Parse(reader.GetString());

    public override void Write(Utf8JsonWriter writer, LTag value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
