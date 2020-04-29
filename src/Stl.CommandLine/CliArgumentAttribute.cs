using System;

namespace Stl.CommandLine
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Property)]
    public class CliArgumentAttribute : Attribute
    {
        public string Template { get; } = "{0}";
        public string DefaultValue { get; set; } = "";
        public bool IsRequired { get; set; }
        public Type? FormatterType { get; set; } = null;
        public double Priority { get; set; } 

        public CliArgumentAttribute() { } 
        public CliArgumentAttribute(string template) 
            => Template = template;
    }
}
