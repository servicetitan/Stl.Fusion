using Newtonsoft.Json;

namespace Stl.Reflection.Internal;

public class TypeRefNewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<TypeRef>
{
    public override void WriteJson(
        JsonWriter writer, TypeRef value, 
        Newtonsoft.Json.JsonSerializer serializer)
        => writer.WriteValue(value.AssemblyQualifiedName.Value);

    public override TypeRef ReadJson(
        JsonReader reader, Type objectType, TypeRef existingValue, bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
        => new((string?) reader.Value!);
}
