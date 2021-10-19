using Newtonsoft.Json;

namespace Stl.Text.Internal;

public class SymbolNewtonsoftJsonConverter : JsonConverter<Symbol>
{
    public override void WriteJson(JsonWriter writer, Symbol value, JsonSerializer serializer)
        => writer.WriteValue(value.Value);

    public override Symbol ReadJson(
        JsonReader reader, Type objectType,
        Symbol existingValue, bool hasExistingValue,
        JsonSerializer serializer)
        => new((string?) reader.Value!);
}
