using System;
using Newtonsoft.Json;
using Stl.Reflection;

namespace Stl.Internal
{
    public class TypeRefConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var typeRef = (TypeRef) value;
            writer.WriteValue(typeRef.AssemblyQualifiedName);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) 
            // ReSharper disable once HeapView.BoxingAllocation
            => new TypeRef((string) reader.Value);

        public override bool CanConvert(Type objectType) 
            => objectType == typeof(TypeRef);
    }
}
