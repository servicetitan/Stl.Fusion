using System;

namespace Stl.ParametersSerializer
{
    public class CliValueAttribute : Attribute
    {
        public string Value { get; }

        public CliValueAttribute(string value) => Value = value;
    }
}