using Newtonsoft.Json;

namespace Stl.Text.Internal;

public class SymbolNewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<Symbol>
{
    public override void WriteJson(
        JsonWriter writer, Symbol value,
        Newtonsoft.Json.JsonSerializer serializer)
        => writer.WriteValue(value.Value);

    public override Symbol ReadJson(
        JsonReader reader, Type objectType, Symbol existingValue, bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
        => new((string?) reader.Value!);
}
