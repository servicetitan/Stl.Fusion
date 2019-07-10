using System;

namespace Stl.CommandLine
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Property)]
    public class CliArgumentAttribute : Attribute
    {
        public string Template { get; } = "{0}";
        public string DefaultValue { get; set; } = "";
        public Type FormatterType { get; set; } = null;

        public CliArgumentAttribute() { } 
        public CliArgumentAttribute(string template) 
            => Template = template;
    }
}
