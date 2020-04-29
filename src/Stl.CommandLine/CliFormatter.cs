using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Stl.Internal;
using Stl.OS;

namespace Stl.CommandLine 
{
    public interface ICliFormatter : IFormatProvider
    {
        CliString Format(object? value, CliArgumentAttribute? argumentAttribute = null);  
    }

    [Serializable]
    public class CliFormatter : ICliFormatter  
    {
        // Workaround against BadImageFormatException in CliFormatter.TrySubstituteValue code
        private static readonly Type CliEnumGenericType = 
            new CliEnum<OSKind>().GetType().GetGenericTypeDefinition();

        public IFormatProvider FormatProvider { get; set; }

        public CliFormatter(IFormatProvider? formatProvider = null) 
            => FormatProvider = formatProvider ?? CultureInfo.InvariantCulture;

        public object? GetFormat(Type? formatType) => FormatProvider.GetFormat(formatType);
        
        public CliString Format(object? value, CliArgumentAttribute? argumentAttribute = null)
        {
            argumentAttribute ??= new CliArgumentAttribute();
            var formatter = GetFormatter(argumentAttribute.FormatterType ?? GetType());
            if (formatter != this)
                return formatter.Format(value, argumentAttribute);

            var template = argumentAttribute.Template!;
            var defaultValue = argumentAttribute.DefaultValue;
            var isRequired = argumentAttribute.IsRequired;

            CliString Format(object o) => string.Format(this, template!, o);
            CliString Default() => isRequired ? throw Errors.MissingCliArgument(template!) : "";

            value = TrySubstituteValue(value);
            return value switch {
                null => Default(),
                string s => s == defaultValue ? Default() : Format(s),
                _ when value.ToString() == defaultValue => Default(),
                // IFormattable is preferred over IEnumerable<IFormattable>
                IFormattable _ => Format(value),
                IEnumerable<IFormattable> sequence => CliString.Concat(sequence.Select(Format)),
                _ => Format(FormatStructure(value)), 
            };
        }

        protected virtual object? TrySubstituteValue(object? value) 
            => value switch {
                null => null,
                bool b => new CliBool(b),
                string s => new CliString(s),
                Enum e => Activator.CreateInstance(
                    CliEnumGenericType.MakeGenericType(e.GetType()), e),
                _ => value
            };

        protected virtual CliString FormatStructure(object value)
        {
            int BaseTypeCount(Type? type) {
                var count = 0;
                for (; type != null; type = type.BaseType) count++;
                return count;
            }

            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            var properties = (
                from property in value.GetType().GetProperties(bindingFlags)
                let attribute = property.GetCustomAttribute<CliArgumentAttribute>(true)
                where attribute != null
                orderby attribute.Priority, BaseTypeCount(property.DeclaringType)
                let propertyValue = property.GetValue(value)
                let formattedPropertyValue = Format(propertyValue, attribute)
                select formattedPropertyValue
                ).ToList();
            return CliString.Concat(properties);
        }

        protected virtual ICliFormatter GetFormatter(Type formatterType)
            => formatterType.IsAssignableFrom(GetType())
                ? this
                // TODO: Slow, we might want to fix this later
                : (ICliFormatter) Activator.CreateInstance(formatterType, FormatProvider)!;
    }
}
