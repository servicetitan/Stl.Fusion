using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;

namespace Stl.Internal
{
    // Used by JSON.NET to serialize dictionary keys of this type
    public class SymbolPathTypeConverter : TypeConverter 
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) 
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {  
            if (destinationType == typeof(string))
                return ((SymbolList) value).Value;
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) 
        {
            if (value is string s)
                // ReSharper disable once HeapView.BoxingAllocation
                return SymbolList.Parse(s);
            return base.ConvertFrom(context, culture, value);
        }
    }
    
    public class SymbolPathJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) 
            => objectType == typeof(SymbolList);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var typeRef = (SymbolList) value!;
            writer.WriteValue(typeRef.Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string) reader.Value!;
            // ReSharper disable once HeapView.BoxingAllocation
            return SymbolList.Parse(value);
        }
    }
}
