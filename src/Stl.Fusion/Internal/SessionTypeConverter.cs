using System.ComponentModel;
using System.Globalization;

namespace Stl.Fusion.Internal;

// Used by JSON.NET to serialize dictionary keys of this type
public class SessionTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string))
            return ((Session?) value)?.Id;
        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    {
        if (value == null)
            return null;
        if (value is string s)
            return new Session(s);
        return base.ConvertFrom(context, culture, value);
    }
}
