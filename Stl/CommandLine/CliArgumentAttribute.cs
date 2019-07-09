using System;

namespace Stl.CommandLine
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CliArgumentAttribute : Attribute
    {
        public string Template { get; } = "{0}";
        public string DefaultValue { get; } = "";
        public Type FormatterType { get; set; } = null;

        public CliArgumentAttribute() { } 
        public CliArgumentAttribute(string template) 
            => Template = template;
        public CliArgumentAttribute(string template, string defaultValue) : this(template) 
            => DefaultValue = defaultValue;
    }
}
