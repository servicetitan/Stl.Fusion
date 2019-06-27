using System;

namespace Stl.ParametersSerializer
{
    public class CliParameterAttribute : Attribute
    {
        public string ParameterPattern { get; }
        
        public string RepeatPattern { get; set; }
        
        public string Separator { get; set; }

        public CliParameterAttribute(string parameterPattern) => ParameterPattern = parameterPattern;
    }
}