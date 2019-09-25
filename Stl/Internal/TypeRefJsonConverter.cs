using System;
using Newtonsoft.Json;
using Stl.Reflection;

namespace Stl.Internal
{
    public class TypeRefJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) 
            => objectType == typeof(TypeRef);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var typeRef = (TypeRef) value!;
            writer.WriteValue(typeRef.AssemblyQualifiedName.Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var assemblyQualifiedName = (string) reader.Value!;
            // ReSharper disable once HeapView.BoxingAllocation
            return new TypeRef(assemblyQualifiedName);
        }
    }
}
