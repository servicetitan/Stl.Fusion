using System;
using System.ComponentModel;
using System.Globalization;

namespace Stl.Serialization.Internal
{
    // Used by JSON.NET to serialize dictionary keys of this type
    public class Base64EncodedTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
                return ((Base64Encoded) value).Encode()!;
            return base.ConvertTo(context, culture, value, destinationType)!;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string s)
                // ReSharper disable once HeapView.BoxingAllocation
                return Base64Encoded.Decode(s);
            return base.ConvertFrom(context, culture, value)!;
        }
    }
}
