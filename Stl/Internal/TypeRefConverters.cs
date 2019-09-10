using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;
using Stl.Reflection;

namespace Stl.Internal
{
    // Used by JSON.NET to serialize dictionary keys of this type
    public class TypeRefTypeConverter : TypeConverter 
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) 
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {  
            if (destinationType == typeof(string))
                return ((TypeRef) value).AssemblyQualifiedName.Value;
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) 
        {
            if (value is string s)
                // ReSharper disable once HeapView.BoxingAllocation
                return new TypeRef(s);
            return base.ConvertFrom(context, culture, value);
        }
    }
    
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
