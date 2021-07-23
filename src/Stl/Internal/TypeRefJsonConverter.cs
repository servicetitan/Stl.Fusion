using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stl.Reflection;

namespace Stl.Internal
{
    public class TypeRefJsonConverter : JsonConverter<TypeRef>
    {
        public override TypeRef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, TypeRef value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.AssemblyQualifiedName.Value);
    }
}
