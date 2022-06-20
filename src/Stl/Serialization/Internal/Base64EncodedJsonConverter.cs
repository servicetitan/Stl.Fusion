namespace Stl.Serialization.Internal;

public class Base64EncodedJsonConverter : JsonConverter<Base64Encoded>
{
    public override Base64Encoded Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Base64Encoded.Decode(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, Base64Encoded value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Encode());
}
