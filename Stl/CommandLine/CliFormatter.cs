using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Stl.CommandLine 
{
    public interface ICliFormatter : IFormatProvider
    {
        CliString Format(object? value, CliArgumentAttribute argumentAttribute = null);  
    }

    [Serializable]
    public class CliFormatter : ICliFormatter  
    {
        public IFormatProvider FormatProvider { get; set; }

        public CliFormatter(IFormatProvider formatProvider = null) 
            => FormatProvider = formatProvider ?? CultureInfo.InvariantCulture;

        public object GetFormat(Type formatType) => FormatProvider.GetFormat(formatType);
        
        public CliString Format(object value, CliArgumentAttribute argumentAttribute = null)
        {
            argumentAttribute ??= new CliArgumentAttribute();
            var formatter = GetFormatter(argumentAttribute.FormatterType ?? GetType());
            if (formatter != this)
                return formatter.Format(value, argumentAttribute);

            var template = argumentAttribute.Template;
            var defaultValue = argumentAttribute.DefaultValue;

            CliString Format(object o) => string.Format(this, template, o);

            value = TrySubstituteValue(value);
            return value switch {
                null => "",
                string s => s == defaultValue ? "" : Format(s),
                _ when value.ToString() == defaultValue => "",
                IEnumerable<IFormattable> sequence => CliString.Concat(sequence.Select(Format)),
                IFormattable _ => Format(value), 
                _ => Format(FormatStructure(value)), 
            };
        }

        protected virtual object TrySubstituteValue(object value) 
            => value switch {
                null => (object) null,
                bool b => new CliBool(b),
                string s => new CliString(s),
                // TODO: This is super slow -- fix this; for now it's better to use CliEnum<T> instead
                Enum e => Activator.CreateInstance(
                    typeof(CliEnum<>).MakeGenericType(e.GetType()), e),
                _ => value,
            };

        protected virtual CliString FormatStructure(object value)
        {
            var parts = new List<CliString>();
            var properties = value.GetType().GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (var property in properties) {
                var propertyArgumentAttribute = property.GetCustomAttribute<CliArgumentAttribute>(true);
                if (propertyArgumentAttribute == null)
                    continue;
                var propertyValue = property.GetValue(value);
                var part = Format(propertyValue, propertyArgumentAttribute);
                parts.Add(part);
            }
            return CliString.Concat(parts);
        }

        protected virtual ICliFormatter GetFormatter(Type formatterType)
            => formatterType.IsAssignableFrom(GetType())
                ? this
                // TODO: Slow, we might want to fix this later
                : (ICliFormatter) Activator.CreateInstance(formatterType, FormatProvider);
    }
}
