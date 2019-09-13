using System;
using System.ComponentModel;
using System.Globalization;
using Stl.Reflection;

namespace Stl.ImmutableModel.Internal
{
    public class DomainKeyTypeConverter : TypeConverter 
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) 
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {  
            if (destinationType == typeof(string)) {
                var v = (DomainKey) value;
                return StringEx.ManyToOne(new [] {v.Domain.AssemblyQualifiedName, v.Key.Value}!);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) 
        {
            if (value is string s) {
                var a = StringEx.OneToMany(s);
                // ReSharper disable once HeapView.BoxingAllocation
                return new DomainKey(new TypeRef(a[0]).Resolve(), new Key(a[1]));
            }
            return base.ConvertFrom(context, culture, value);
        }
    }}
