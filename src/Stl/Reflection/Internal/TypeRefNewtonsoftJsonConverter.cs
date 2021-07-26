using System;
using Newtonsoft.Json;

namespace Stl.Reflection.Internal
{
    public class TypeRefNewtonsoftJsonConverter : JsonConverter<TypeRef>
    {
        public override void WriteJson(JsonWriter writer, TypeRef value, JsonSerializer serializer)
            => writer.WriteValue(value.AssemblyQualifiedName.Value);

        public override TypeRef ReadJson(JsonReader reader, Type objectType, TypeRef existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => new((string?) reader.Value!);
    }
}
